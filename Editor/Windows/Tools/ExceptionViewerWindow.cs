using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows.Tools
{
    /// <summary>
    ///     全局异常查看器窗口
    /// </summary>
    public sealed class ExceptionViewerWindow : EditorWindow
    {
        private readonly List<ExceptionInfo> _exceptions = new();

        // UI 元素引用
        private ScrollView _scrollView;
        private Label _countLabel;

        private void OnEnable()
        {
            // 注册异常处理
            if (Application.isPlaying)
            {
                var scope = GameScope.Instance;
                if (scope != null && scope.Container != null)
                {
                    var dispatcher = scope.Container.Resolve(typeof(IExceptionDispatcher)) as IExceptionDispatcher;
                    dispatcher?.RegisterHandler(OnException);
                }
            }
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

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

            root.Add(toolbar);

            // 间距
            var spacer = new VisualElement();
            spacer.style.height = 5;
            root.Add(spacer);

            // 异常滚动列表
            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.flexGrow = 1;
            root.Add(_scrollView);

            RefreshExceptionList();
        }

        [MenuItem("CFramework/Exception Viewer")]
        public static void ShowWindow()
        {
            GetWindow<ExceptionViewerWindow>("Exception Viewer");
        }

        private void OnException(Exception ex)
        {
            _exceptions.Add(new ExceptionInfo
            {
                Time = DateTime.Now,
                Message = ex.Message,
                StackTrace = ex.StackTrace
            });

            RefreshExceptionList();
            Repaint();
        }

        /// <summary>
        ///     刷新异常列表显示
        /// </summary>
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

        /// <summary>
        ///     创建单个异常项的 UI 元素
        /// </summary>
        private VisualElement CreateExceptionItem(ExceptionInfo info)
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

        private struct ExceptionInfo
        {
            public DateTime Time;
            public string Message;
            public string StackTrace;
        }
    }
}
