using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     FrameworkSettings 自定义 Inspector
    /// </summary>
    [CustomEditor(typeof(FrameworkSettings))]
    internal sealed class FrameworkSettingsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // 使用 InspectorElement 绘制默认 Inspector 内容
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            return root;
        }

        /// <summary>
        ///     FrameworkSettings 创建工具（菜单入口）
        /// </summary>
        [MenuItem("CFramework/创建框架设置", priority = 400)]
        public static void CreateFrameworkSettings()
        {
            var settings = ScriptableObject.CreateInstance<FrameworkSettings>();

            var path = EditorUtility.SaveFilePanelInProject(
                "Save FrameworkSettings",
                "FrameworkSettings",
                "asset",
                "Save FrameworkSettings asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = settings;
            }
        }
    }
}
