using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     FrameworkSettings 自定义 Inspector
    /// </summary>
    [CustomEditor(typeof(FrameworkSettings))]
    internal sealed class FrameworkSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        /// <summary>
        ///     FrameworkSettings 创建工具（菜单入口）
        /// </summary>
        [MenuItem("CFramework/CreateSettings")]
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
