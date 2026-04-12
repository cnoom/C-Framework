using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Windows.Addressable
{
    /// <summary>
    ///     Addressables 配置预览窗口
    /// </summary>
    public sealed class AddressableConfigPreviewWindow : OdinEditorWindow
    {
        private string _previewContent = "";
        private Vector2 _scrollPosition;

        public static void ShowWindow(string content)
        {
            var window = GetWindow<AddressableConfigPreviewWindow>();
            window.titleContent = new GUIContent("配置预览");
            window.minSize = new Vector2(600, 400);
            window._previewContent = content;
            window.Show();
        }

        protected override void OnImGUI()
        {
            base.OnImGUI();

            // 添加滚动视图
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 使用 TextArea 显示预览内容，设置为只读
            var style = new GUIStyle(EditorStyles.textField)
            {
                wordWrap = true,
                richText = true,
                fontSize = 12,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.TextArea(_previewContent, style, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndScrollView();

            // 底部按钮
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("复制到剪贴板", GUILayout.Width(120), GUILayout.Height(30)))
                {
                    EditorGUIUtility.systemCopyBuffer = _previewContent;
                    Debug.Log("[AddressableConfigPreviewWindow] 预览内容已复制到剪贴板");
                }

                if (GUILayout.Button("关闭", GUILayout.Width(80), GUILayout.Height(30))) Close();
            }
        }
    }
}