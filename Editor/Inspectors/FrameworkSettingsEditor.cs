using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     FrameworkSettings 创建工具
    /// </summary>
    public static class FrameworkSettingsEditor
    {
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