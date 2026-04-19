#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace CFramework.Editor.Utilities
{
    /// <summary>
    ///     编辑器 USS 样式表加载工具
    ///     搜索所有 StyleSheet 资源，按文件名匹配目标 USS
    /// </summary>
    internal static class EditorStyleSheet
    {
        /// <summary>
        ///     按文件名查找并加载 USS 样式表
        /// </summary>
        public static StyleSheet Find(string ussFileName)
        {
            var guids = AssetDatabase.FindAssets("t:StyleSheet");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(ussFileName))
                {
                    return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                }
            }
            return null;
        }
    }
}
#endif
