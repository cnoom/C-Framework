#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows.Addressable
{
    /// <summary>
    ///     Addressables 配置预览窗口（Odin 版本）
    /// </summary>
    public sealed class AddressableConfigPreviewWindow : OdinEditorWindow
    {
        private string _previewContent = "";

        public static void ShowWindow(string content)
        {
            var window = GetWindow<AddressableConfigPreviewWindow>();
            window.titleContent = new GUIContent("配置预览");
            window.minSize = new Vector2(600, 400);
            window._previewContent = content;
            window.Show();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 滚动文本区域
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;

            var textLabel = new Label(_previewContent);
            textLabel.style.whiteSpace = WhiteSpace.Normal;
            textLabel.style.fontSize = 12;
            textLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            textLabel.style.paddingTop = 10;
            textLabel.style.paddingBottom = 10;
            textLabel.style.paddingLeft = 10;
            textLabel.style.paddingRight = 10;
            scrollView.Add(textLabel);

            root.Add(scrollView);

            // 间距
            var spacer = new VisualElement();
            spacer.style.height = 10;
            root.Add(spacer);

            // 底部按钮栏
            var buttonBar = new VisualElement();
            buttonBar.style.flexDirection = FlexDirection.Row;
            buttonBar.style.justifyContent = Justify.FlexEnd;

            var copyButton = new Button(() =>
            {
                EditorGUIUtility.systemCopyBuffer = _previewContent;
                Debug.Log("[AddressableConfigPreviewWindow] 预览内容已复制到剪贴板");
            })
            {
                text = "复制到剪贴板"
            };
            copyButton.style.width = 120;
            copyButton.style.height = 30;
            buttonBar.Add(copyButton);

            var closeButton = new Button(Close)
            {
                text = "关闭"
            };
            closeButton.style.width = 80;
            closeButton.style.height = 30;
            closeButton.style.marginLeft = 5;
            buttonBar.Add(closeButton);

            root.Add(buttonBar);
        }
    }
}
#else
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows.Addressable
{
    /// <summary>
    ///     Addressables 配置预览窗口（默认实现，不依赖 Odin）
    /// </summary>
    public sealed class AddressableConfigPreviewWindow : EditorWindow
    {
        private string _previewContent = "";

        public static void ShowWindow(string content)
        {
            var window = GetWindow<AddressableConfigPreviewWindow>();
            window.titleContent = new GUIContent("配置预览");
            window.minSize = new Vector2(600, 400);
            window._previewContent = content;
            window.Show();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 滚动文本区域
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;

            var textLabel = new Label(_previewContent);
            textLabel.style.whiteSpace = WhiteSpace.Normal;
            textLabel.style.fontSize = 12;
            textLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            textLabel.style.paddingTop = 10;
            textLabel.style.paddingBottom = 10;
            textLabel.style.paddingLeft = 10;
            textLabel.style.paddingRight = 10;
            scrollView.Add(textLabel);

            root.Add(scrollView);

            // 间距
            var spacer = new VisualElement();
            spacer.style.height = 10;
            root.Add(spacer);

            // 底部按钮栏
            var buttonBar = new VisualElement();
            buttonBar.style.flexDirection = FlexDirection.Row;
            buttonBar.style.justifyContent = Justify.FlexEnd;

            var copyButton = new Button(() =>
            {
                EditorGUIUtility.systemCopyBuffer = _previewContent;
                Debug.Log("[AddressableConfigPreviewWindow] 预览内容已复制到剪贴板");
            })
            {
                text = "复制到剪贴板"
            };
            copyButton.style.width = 120;
            copyButton.style.height = 30;
            buttonBar.Add(copyButton);

            var closeButton = new Button(Close)
            {
                text = "关闭"
            };
            closeButton.style.width = 80;
            closeButton.style.height = 30;
            closeButton.style.marginLeft = 5;
            buttonBar.Add(closeButton);

            root.Add(buttonBar);
        }
    }
}
#endif
