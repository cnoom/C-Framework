using System;
using System.Collections.Generic;
using System.Threading;
using CFramework;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     UI 服务实现
    ///     <para>管理 UI 面板的加载、缓存、生命周期和导航栈</para>
    ///     <para>约定：Prefab 名称 = 类名（typeof(T).Name），自动推导 Addressable Key</para>
    /// </summary>
    public sealed class UIService : IUIService, IAsyncInitializable, IDisposable
    {
        private readonly IAssetService _assetService;
        private readonly IObjectResolver _resolver;
        private readonly LinkedList<string> _navigationStack = new();
        private readonly Subject<string> _panelClosed = new();
        private readonly Subject<string> _panelOpened = new();

        private readonly Dictionary<string, UIPanelData> _panels = new();
        private readonly UISettings _settings;
        private readonly UniTaskCompletionSource _uiRootReady = new();
        private CancellationTokenSource _cancellationTokenSource;
        [Inject] ILogger _logger;

        private int _maxStackCapacity = 10;

        private Transform _uiRoot;
        private AssetHandle _uiRootHandle;

        public UIService(IAssetService assetService, IObjectResolver resolver, UISettings settings)
        {
            _assetService = assetService;
            _resolver = resolver;
            _settings = settings;
            _maxStackCapacity = settings.MaxNavigationStack;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        ///     异步初始化 UIRoot（IAsyncInitializable 实现）
        /// </summary>
        public async UniTask InitializeAsync()
        {
            try
            {
                // 尝试通过 Addressable 加载 UIRoot Prefab
                var uiRootAddress = _settings.UIRootAddress;
                if (!string.IsNullOrEmpty(uiRootAddress))
                    try
                    {
                        _uiRootHandle =
                            await _assetService.LoadAsync<GameObject>(uiRootAddress, _cancellationTokenSource.Token);
                        var prefab = _uiRootHandle.As<GameObject>();
                        if (prefab != null)
                        {
                            var rootGo = Object.Instantiate(prefab);
                            rootGo.name = "[UIRoot]";
                            Object.DontDestroyOnLoad(rootGo);
                            _uiRoot = rootGo.transform;
                            Canvas = rootGo.GetComponent<Canvas>();

                            if (Canvas == null)
                                LogUtility.Warning("UIService", "UIRoot Prefab 上未找到 Canvas 组件，UI 可能无法正确渲染");

                            _uiRootReady.TrySetResult();
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogWithLevelColor("UIService",
                            $"UIRoot Prefab 加载失败（Address: {uiRootAddress}），回退到代码创建。原因: {e.Message}", LogLevel.Warning);
                        _uiRootHandle.Dispose();
                        _uiRootHandle = default;
                    }

                // Fallback：代码创建带 Canvas 的 UIRoot
                CreateFallbackUIRoot();
                _uiRootReady.TrySetResult();
            }
            catch (Exception e)
            {
                _logger.LogWithLevelColor("UIService", $"Initialize failed: {e.Message}", LogLevel.Error);
                _uiRootReady.TrySetException(e);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            CloseAll();

            // 销毁所有缓存的面板
            foreach (var kvp in _panels)
            {
                try
                {
                    kvp.Value.UI.OnDestroy();
                }
                catch (Exception e)
                {
                    LogUtility.Warning("UIService", $"OnDestroy 异常: {kvp.Key}, {e.Message}");
                }

                if (kvp.Value.GameObject != null) Object.Destroy(kvp.Value.GameObject);
                kvp.Value.Handle.Dispose();
            }

            _panels.Clear();
            _panelOpened.Dispose();
            _panelClosed.Dispose();

            // 释放 UIRoot
            if (_uiRoot != null) Object.Destroy(_uiRoot.gameObject);

            _uiRootHandle.Dispose();
        }

        public int StackCount => _navigationStack.Count;

        public int MaxStackCapacity
        {
            get => _maxStackCapacity;
            set => _maxStackCapacity = Mathf.Max(1, value);
        }

        public Observable<string> OnPanelOpened => _panelOpened;

        public Observable<string> OnPanelClosed => _panelClosed;

        /// <summary>
        ///     UIRoot 上的 Canvas 组件
        /// </summary>
        public Canvas Canvas { get; private set; }

        /// <summary>
        ///     通过类型打开面板
        ///     <para>Prefab 名称 = typeof(T).Name 作为 Addressable Key</para>
        /// </summary>
        public async UniTask<T> OpenAsync<T>() where T : IUI, new()
        {
            // 等待 UIRoot 初始化完成（事件信号，无轮询）
            var readyTask = _uiRootReady.Task;
            if (readyTask.Status != UniTaskStatus.Pending)
            {
                // UIRoot 已初始化完成，直接跳过等待
            }
            else
            {
                // UIRoot 尚未就绪，等待（超时保护 30 秒）
                var timeout = UniTask.Delay(TimeSpan.FromSeconds(30));
                if (await UniTask.WhenAny(readyTask, timeout) == 1)
                    throw new InvalidOperationException("[UIService] 等待 UIRoot 初始化超时（30秒），请检查 UIRoot 配置");
            }
            var panelKey = typeof(T).Name;

            // 已缓存的面板，直接显示
            if (_panels.TryGetValue(panelKey, out var existing))
            {
                existing.GameObject.SetActive(true);
                existing.IsOpen = true;
                existing.UI.OnShow();

                PushToNavigationStack(panelKey);
                _panelOpened.OnNext(panelKey);

                return (T)existing.UI;
            }

            // 加载并实例化 Prefab
            var handle = await _assetService.LoadAsync<GameObject>(panelKey, _cancellationTokenSource.Token);
            var go = Object.Instantiate(handle.Asset, _uiRoot) as GameObject;

            if (go == null)
            {
                handle.Dispose();
                throw new InvalidOperationException($"[UIService] Prefab 实例化失败: {panelKey}");
            }

            var binder = go.GetComponent<UIBinder>();
            if (binder == null)
            {
                Object.Destroy(go);
                handle.Dispose();
                throw new InvalidOperationException($"[UIService] Prefab 上未找到 UIBinder 组件: {panelKey}");
            }

            // 创建 IUI 实例、通过 DI 容器执行属性/方法注入、注入组件并初始化
            var ui = new T();
            _resolver.Inject(ui);
            ui.InjectUI(binder);
            ui.OnCreate();

            // 缓存面板数据
            var panelData = new UIPanelData
            {
                UI = ui,
                GameObject = go,
                Binder = binder,
                Handle = handle,
                IsOpen = true
            };
            _panels[panelKey] = panelData;

            // 显示面板
            ui.OnShow();

            PushToNavigationStack(panelKey);
            _panelOpened.OnNext(panelKey);

            return ui;
        }

        /// <summary>
        ///     通过类型关闭面板（隐藏并缓存）
        /// </summary>
        public void Close<T>() where T : IUI
        {
            var panelKey = typeof(T).Name;
            ClosePanel(panelKey);
        }

        /// <summary>
        ///     关闭所有面板
        /// </summary>
        public void CloseAll()
        {
            foreach (var kvp in _panels)
                if (kvp.Value.IsOpen)
                {
                    kvp.Value.UI.OnHide();
                    kvp.Value.IsOpen = false;
                    if(kvp.Value.GameObject)
                        kvp.Value.GameObject.SetActive(false);
                }

            _navigationStack.Clear();
        }

        /// <summary>
        ///     返回上一层面板
        /// </summary>
        public void GoBack()
        {
            if (_navigationStack.Count <= 1) return;

            var current = _navigationStack.Last.Value;
            ClosePanel(current);
        }

        /// <summary>
        ///     创建兜底的 UIRoot（带 Canvas + CanvasScaler + GraphicRaycaster）
        /// </summary>
        private void CreateFallbackUIRoot()
        {
            var rootGo = new GameObject("[UIRoot]");
            Object.DontDestroyOnLoad(rootGo);
            _uiRoot = rootGo.transform;

            // 添加 Canvas
            Canvas = rootGo.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 0;

            // 添加 CanvasScaler（标准适配）
            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 添加 GraphicRaycaster（接收点击事件）
            rootGo.AddComponent<GraphicRaycaster>();

            LogUtility.Info("UIService", "已创建代码兜底 UIRoot（Canvas + CanvasScaler + GraphicRaycaster）");
        }

        /// <summary>
        ///     关闭面板（隐藏并缓存）
        /// </summary>
        private void ClosePanel(string panelKey)
        {
            if (!_panels.TryGetValue(panelKey, out var panel)) return;

            if (panel.IsOpen)
            {
                panel.UI.OnHide();
                panel.IsOpen = false;
                if (panel.GameObject != null)
                    panel.GameObject.SetActive(false);
            }

            _navigationStack.Remove(panelKey);
            _panelClosed.OnNext(panelKey);
        }

        /// <summary>
        ///     彻底销毁面板（调用 OnDestroy、释放资源、销毁 GameObject）
        /// </summary>
        private void DestroyPanel(string panelKey)
        {
            if (!_panels.TryGetValue(panelKey, out var panel)) return;

            panel.UI.OnDestroy();
            _navigationStack.Remove(panelKey);
            _panels.Remove(panelKey);

            if (panel.GameObject != null) Object.Destroy(panel.GameObject);

            panel.Handle.Dispose();
        }

        /// <summary>
        ///     将面板推入导航栈
        /// </summary>
        private void PushToNavigationStack(string panelKey)
        {
            // 如果已在栈中，移到栈顶
            _navigationStack.Remove(panelKey);

            // 超出容量时销毁最旧的面板
            while (_navigationStack.Count >= _maxStackCapacity)
            {
                var oldest = _navigationStack.First.Value;
                DestroyPanel(oldest);
            }

            _navigationStack.AddLast(panelKey);
        }

        /// <summary>
        ///     面板运行时数据
        /// </summary>
        private sealed class UIPanelData
        {
            public IUI UI { get; set; }
            public GameObject GameObject { get; set; }
            public UIBinder Binder { get; set; }
            public AssetHandle Handle { get; set; }
            public bool IsOpen { get; set; }
        }
    }
}