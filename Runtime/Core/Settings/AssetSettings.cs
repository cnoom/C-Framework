using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     资源模块配置
    /// </summary>
    [CreateAssetMenu(fileName = "AssetSettings", menuName = "CFramework/Asset Settings")]
    public sealed class AssetSettings : ScriptableObject
    {
        private const string DefaultPath = "AssetSettings";

        [Tooltip("内存预算(MB)")]
        public int MemoryBudgetMB = 512;

        [Tooltip("每帧最大加载数量")]
        public int MaxLoadPerFrame = 5;

        /// <summary>
        ///     加载默认配置
        /// </summary>
        public static AssetSettings LoadDefault()
        {
            var settings = Resources.Load<AssetSettings>(DefaultPath);
            if (settings == null)
            {
                settings = CreateInstance<AssetSettings>();
                LogUtility.Warning("CFramework",
                    $"AssetSettings not found at Resources/{DefaultPath}, using default values.");
            }

            return settings;
        }
    }
}
