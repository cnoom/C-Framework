using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

using VContainer;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     游戏全局作用域，管理整个游戏的生命周期
    ///     <para>支持通过 IInstaller 在任意时刻动态注册服务模块</para>
    ///     <para>使用流程：AddInstaller → Create → Initialize（手动触发 Build）</para>
    ///     <para>不使用 AutoRun，用户需显式调用 Initialize() 以完成容器构建</para>
    /// </summary>
    public sealed class GameScope : LifetimeScope
    {
        // 所有已注册的动态安装器
        private static readonly List<IInstaller> _additionalInstallers = new();

        // 线程安全锁
        private static readonly object _installerLock = new();

        // 框架内置安装器（按顺序执行）
        private static readonly IInstaller[] _builtInInstallers =
        {
            new CoreServiceInstaller(),
            new FrameworkModuleInstaller()
        };

        [SerializeField] private FrameworkSettings _settings;

        /// <summary>
        ///     是否已初始化（调用过 Initialize）
        /// </summary>
        private bool _isInitialized;

        public static GameScope Instance { get; private set; }

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 不调用 base.Awake()，不自动 Build
            // 用户需显式调用 Initialize() 以完成容器构建
        }

        /// <summary>
        ///     核心初始化：构建 DI 容器并解析所有框架服务
        ///     <para>仅供 InitializeAsync 内部调用，不对外暴露</para>
        /// </summary>
        private void InitializeCore()
        {
            if (_isInitialized)
            {
                LogUtility.Warning("GameScope", "Initialize 已被调用，忽略重复调用");
                return;
            }

            Build();
            ResolveFrameworkServices();
            _isInitialized = true;
        }

        /// <summary>
        ///     异步初始化框架：构建 DI 容器、解析服务，并等待所有异步服务就绪
        ///     <para>确保所有异步服务（UI、Audio 等）完全就绪后才返回</para>
        ///     <para>典型用法：await GameScope.Create(settings).InitializeAsync()</para>
        /// </summary>
        public async UniTask InitializeAsync()
        {
            InitializeCore();

            // 等待所有实现 IAsyncInitializable 的服务完成异步初始化
            var asyncServices = Container.Resolve<IEnumerable<IAsyncInitializable>>();
            if (asyncServices.Any())
                await UniTask.WhenAll(asyncServices.Select(s => s.InitializeAsync()));
        }

        protected override void OnDestroy()
        {
            var wasInitialized = false;

            if (Instance == this)
            {
                wasInitialized = _isInitialized;
                Instance = null;
                _isInitialized = false;

                // 清理静态缓存，防止关机阶段访问已释放服务
                ServiceCache.Logger = null;
                ServiceCache.ConfigService = null;
                LogUtility.Logger = null;
            }

            // 仅在已初始化时执行 base.OnDestroy（DisposeCore 需要 Container 存在）
            if (wasInitialized)
                base.OnDestroy();
        }

        /// <summary>
        ///     在 Domain Reload 前自动清理静态安装器列表，防止残留
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            lock (_installerLock)
            {
                _additionalInstallers.Clear();
            }
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // 加载 FrameworkSettings
            var settings = _settings ?? FrameworkSettings.LoadDefault();

            // 注册 FrameworkSettings（向后兼容）
            builder.RegisterInstance(settings);

            // 注册各模块 Settings（子 Settings 为空时自动 fallback）
            builder.RegisterInstance(settings.GetLogSettings());
            builder.RegisterInstance(settings.GetAssetSettings());
            builder.RegisterInstance(settings.GetUISettings());
            builder.RegisterInstance(settings.GetAudioSettings());
            builder.RegisterInstance(settings.GetSaveSettings());
            builder.RegisterInstance(settings.GetPoolSettings());
            builder.RegisterInstance(settings.GetConfigSettings());

            // 安装框架内置服务
            foreach (var installer in _builtInInstallers) builder.Install(installer);

            // 安装动态注册的服务（快照后遍历，避免在锁内执行 DI 注册）
            IInstaller[] snapshot;
            lock (_installerLock)
            {
                snapshot = _additionalInstallers.ToArray();
            }

            foreach (var installer in snapshot) builder.Install(installer);
        }

        /// <summary>
        ///     解析框架核心服务并初始化静态桥接
        ///     <para>LogUtility.Logger / ServiceCache 均在此处初始化，供非 DI 管理的对象使用</para>
        /// </summary>
        private void ResolveFrameworkServices()
        {
            Logger = Container.Resolve<ILogger>();
            LogUtility.Logger = Logger;
            ServiceCache.Logger = Logger;
            ServiceCache.ConfigService = Container.Resolve<IConfigService>();
        }

        /// <summary>
        ///     创建游戏作用域（不自动构建）
        ///     <para>创建后需调用 InitializeAsync() 以构建容器并解析服务</para>
        ///     <para>典型用法：await GameScope.Create().InitializeAsync()</para>
        /// </summary>
        /// <param name="settings">框架设置，为 null 时加载默认设置</param>
        /// <returns>未初始化的 GameScope 实例</returns>
        public static GameScope Create(FrameworkSettings settings = null)
        {
            settings ??= FrameworkSettings.LoadDefault();
            var go = new GameObject("[GameScope]");
            go.SetActive(false);
            var scope = go.AddComponent<GameScope>();
            scope._settings = settings;
            scope.autoRun = false;
            go.SetActive(true);
            return scope;
        }

        /// <summary>
        ///     内部日志桥接（仅供 LogUtility 使用，不对外暴露）
        /// </summary>
        internal ILogger Logger { get; private set; }

        #region 动态注册

        /// <summary>
        ///     添加动态安装器
        ///     <para>Initialize 前：排队等待首次构建</para>
        ///     <para>Initialize 后：自动触发容器重建</para>
        ///     <para>示例：GameScope.AddInstaller(new ActionInstaller(b => b.Register&lt;IFoo, Foo&gt;()))</para>
        /// </summary>
        /// <param name="installer">安装器实例</param>
        public static void AddInstaller(params IInstaller[] installer)
        {
            if (installer == null) throw new ArgumentNullException(nameof(installer));

            lock (_installerLock)
            {
                foreach (var i in installer)
                {
                    _additionalInstallers.Add(i);
                }
            }

            // 如果 GameScope 已初始化，触发容器重建
            if (Instance != null && Instance._isInitialized) Instance.RebuildContainer();
        }

        /// <summary>
        ///     添加委托式安装器
        ///     <para>Initialize 前：排队等待首次构建</para>
        ///     <para>Initialize 后：自动触发容器重建</para>
        /// </summary>
        /// <param name="installAction">注册动作</param>
        public static void AddInstaller(Action<IContainerBuilder> installAction)
        {
            if (installAction == null) throw new ArgumentNullException(nameof(installAction));

            lock (_installerLock)
            {
                _additionalInstallers.Add(new ActionInstaller(installAction));
            }

            // 如果 GameScope 已初始化，触发容器重建
            if (Instance != null && Instance._isInitialized) Instance.RebuildContainer();
        }

        /// <summary>
        ///     移除指定安装器
        ///     <para>移除后需要调用 RebuildIfNeeded() 或等待下次自动重建才能生效</para>
        /// </summary>
        /// <param name="installer">要移除的安装器</param>
        /// <returns>是否移除成功</returns>
        public static bool RemoveInstaller(IInstaller installer)
        {
            lock (_installerLock)
            {
                return _additionalInstallers.Remove(installer);
            }
        }

        /// <summary>
        ///     清除所有动态安装器
        ///     <para>清除后需要调用 RebuildContainer() 才能使变更生效</para>
        /// </summary>
        public static void ClearInstallers()
        {
            lock (_installerLock)
            {
                _additionalInstallers.Clear();
            }
        }

        /// <summary>
        ///     手动触发容器重建
        ///     <para>重新执行所有安装器（内置 + 动态），重建 DI 容器</para>
        ///     <para>注意：重建会释放所有已注册的 IDisposable 服务并重新创建</para>
        /// </summary>
        public void RebuildContainer()
        {
            DisposeCore();
            Build();
            ResolveFrameworkServices();
        }

        #endregion
    }
}