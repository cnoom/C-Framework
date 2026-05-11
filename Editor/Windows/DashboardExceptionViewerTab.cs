using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows
{
    /// <summary>
    ///     Dashboard 异常查看 Tab 的内容构建器
    ///     从 ExceptionViewerWindow 提取，嵌入 Dashboard 使用
    ///     在 Play Mode 下自动注册异常处理器并实时显示异常
    /// </summary>
    public class DashboardExceptionViewerTab
    {
        #region 数据

        private readonly List<ExceptionInfo> _exceptions = new();
        private IDisposable _handlerSubscription;

        #endregion

        #region 控件引用

        private ScrollView _scrollView;
        private Label _countLabel;
        private Label _statusLabel;

        #endregion

        #region 公开接口

        /// <summary>
        ///     创建 Tab 内容
        /// </summary>
        public VisualElement CreateContent()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            // 工具栏
            var toolbar = new Toolbar();
            toolbar.style.flexDirection = FlexDirection.Row;

            var clearButton = new ToolbarButton(() =>
            {
                _exceptions.Clear();
                RefreshExceptionList();
            })
            {
                text = "Clear"
            };
            toolbar.Add(clearButton);

            toolbar.Add(new VisualElement { style = { flexGrow = 1 } });

            _countLabel = new Label($"Count: {_exceptions.Count}");
            _countLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _countLabel.style.marginLeft = 5;
            toolbar.Add(_countLabel);

            container.Add(toolbar);

            // 运行时状态提示
            _statusLabel = new Label("异常捕获将在进入 Play Mode 时自动激活。");
            _statusLabel.AddToClassList("runtime-status-label");
            container.Add(_statusLabel);

            // 异常滚动列表
            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.flexGrow = 1;
            container.Add(_scrollView);

            RefreshExceptionList();

            return container;
        }

        /// <summary>
        ///     启用（Tab 激活 / 进入 Play Mode 时调用）
        /// </summary>
        public void Enable()
        {
            RegisterExceptionHandler();
        }

        /// <summary>
        ///     禁用（Tab 切换离开 / 窗口关闭时调用）
        /// </summary>
        public void Disable()
        {
            UnregisterExceptionHandler();
        }

        /// <summary>
        ///     Play Mode 状态变化时调用
        /// </summary>
        public void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    RegisterExceptionHandler();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    UnregisterExceptionHandler();
                    break;
            }

            UpdateStatusLabel();
        }

        #endregion

        #region 异常处理器注册

        private void RegisterExceptionHandler()
        {
            if (_handlerSubscription != null) return;

            if (!Application.isPlaying) return;

            var scope = GameScope.Instance;
            if (scope != null && scope.Container != null)
            {
                var dispatcher = scope.Container.Resolve(typeof(IExceptionDispatcher)) as IExceptionDispatcher;
                if (dispatcher != null)
                {
                    _handlerSubscription = dispatcher.RegisterHandler(OnException);
                    Debug.Log("[ExceptionViewer] 异常处理器已注册");
                }
            }

            UpdateStatusLabel();
        }

        private void UnregisterExceptionHandler()
        {
            if (_handlerSubscription == null) return;

            _handlerSubscription.Dispose();
            _handlerSubscription = null;
        }

        private void UpdateStatusLabel()
        {
            if (_statusLabel == null) return;

            if (!Application.isPlaying)
            {
                _statusLabel.text = "异常捕获将在进入 Play Mode 时自动激活。";
                _statusLabel.style.display = DisplayStyle.Flex;
            }
            else if (_handlerSubscription == null)
            {
                _statusLabel.text = "等待 GameScope 初始化...";
                _statusLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                _statusLabel.style.display = DisplayStyle.None;
            }
        }

        #endregion

        #region UI 刷新

        private void OnException(Exception ex)
        {
            _exceptions.Add(new ExceptionInfo
            {
                Time = DateTime.Now,
                Message = ex.Message,
                StackTrace = ex.StackTrace
            });

            RefreshExceptionList();
        }

        private void RefreshExceptionList()
        {
            if (_scrollView == null) return;

            _scrollView.Clear();

            // 倒序显示，最新的在最上面
            for (var i = _exceptions.Count - 1; i >= 0; i--)
            {
                var info = _exceptions[i];
                _scrollView.Add(CreateExceptionItem(info));
            }

            // 更新计数标签
            if (_countLabel != null)
            {
                _countLabel.text = $"Count: {_exceptions.Count}";
            }
        }

        private static VisualElement CreateExceptionItem(ExceptionInfo info)
        {
            var container = new Box();
            container.style.marginBottom = 2;
            container.style.paddingTop = 5;
            container.style.paddingBottom = 5;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.unityBackgroundImageTintColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            // 异常标题（时间 + 消息）
            var titleLabel = new Label($"[{info.Time:HH:mm:ss}] {info.Message}");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.whiteSpace = WhiteSpace.Normal;
            container.Add(titleLabel);

            // 复制堆栈跟踪按钮
            var copyButton = new Button(() =>
            {
                EditorGUIUtility.systemCopyBuffer = info.StackTrace;
            })
            {
                text = "Copy Stack Trace"
            };
            copyButton.style.alignSelf = Align.FlexEnd;
            copyButton.style.marginTop = 4;
            copyButton.style.unityBackgroundImageTintColor = Color.clear;
            copyButton.style.color = new Color(0.4f, 0.7f, 1f);
            container.Add(copyButton);

            return container;
        }

        #endregion

        #region 嵌套类型

        private struct ExceptionInfo
        {
            public DateTime Time;
            public string Message;
            public string StackTrace;
        }

        #endregion
    }
}
