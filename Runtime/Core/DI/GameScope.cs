using System;
using System.Collections.Generic;
using UnityEngine;
#if CFRAMEWORK_UI
using CFramework.Runtime.UI;
#endif
using VContainer;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     游戏全局作用域，管理整个游戏的生命周期
    ///     <para>支持通过 IInstaller 在任意时刻动态注册服务模块</para>
    ///     <para>Awake 前：安装器在 Configure 中执行</para>
    ///     <para>Awake 后：添加安装器会自动重建容器（ReBuild）</para>
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
        ///     是否已完成首次构建
        /// </summary>
        private bool _isBuilt;

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

            base.Awake();
            ResolveFrameworkServices();
            _isBuilt = true;
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                _isBuilt = false;
            }

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
            // 注册 FrameworkSettings
            if (_settings != null)
            {
                builder.RegisterInstance(_settings);
            }
            else
            {
                var defaultSettings = FrameworkSettings.LoadDefault();
                builder.RegisterInstance(defaultSettings);
            }

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
        ///     解析框架公共服务到属性
        /// </summary>
        private void ResolveFrameworkServices()
        {
            Logger = Container.Resolve<ILogger>();
            EventBus = Container.Resolve<IEventBus>();
            ExceptionDispatcher = Container.Resolve<IExceptionDispatcher>();
            AssetService = Container.Resolve<IAssetService>();
#if CFRAMEWORK_AUDIO
            AudioService = Container.Resolve<IAudioService>();
#endif
            SceneService = Container.Resolve<ISceneService>();
            ConfigService = Container.Resolve<IConfigService>();
            SaveService = Container.Resolve<ISaveService>();
#if CFRAMEWORK_UI
            UIService = Container.Resolve<IUIService>();
#endif
        }

        /// <summary>
        ///     创建游戏作用域
        /// </summary>
        public static GameScope Create(FrameworkSettings settings = null)
        {
            settings ??= FrameworkSettings.LoadDefault();
            var go = new GameObject("[GameScope]");
            go.SetActive(false);
            var scope = go.AddComponent<GameScope>();
            scope._settings = settings;
            go.SetActive(true);
            return scope;
        }

        #region 公共服务属性

        public ILogger Logger { get; private set; }
        public IEventBus EventBus { get; private set; }
        public IExceptionDispatcher ExceptionDispatcher { get; private set; }
        public IAssetService AssetService { get; private set; }
#if CFRAMEWORK_AUDIO
        public IAudioService AudioService { get; private set; }
#endif
        public ISceneService SceneService { get; private set; }
        public IConfigService ConfigService { get; private set; }
        public ISaveService SaveService { get; private set; }
#if CFRAMEWORK_UI
        public IUIService UIService { get; private set; }
#endif

        #endregion

        #region 动态注册

        /// <summary>
        ///     添加动态安装器
        ///     <para>可在任意时刻调用：Awake 前会排队等待首次构建，Awake 后自动触发容器重建</para>
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

            // 如果 GameScope 已构建，触发容器重建
            if (Instance != null && Instance._isBuilt) Instance.RebuildContainer();
        }

        /// <summary>
        ///     添加委托式安装器
        ///     <para>可在任意时刻调用：Awake 前会排队等待首次构建，Awake 后自动触发容器重建</para>
        /// </summary>
        /// <param name="installAction">注册动作</param>
        public static void AddInstaller(Action<IContainerBuilder> installAction)
        {
            if (installAction == null) throw new ArgumentNullException(nameof(installAction));

            lock (_installerLock)
            {
                _additionalInstallers.Add(new ActionInstaller(installAction));
            }

            // 如果 GameScope 已构建，触发容器重建
            if (Instance != null && Instance._isBuilt) Instance.RebuildContainer();
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