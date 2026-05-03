using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     配置模块配置
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigSettings", menuName = "CFramework/Config Settings")]
    public sealed class ConfigSettings : ScriptableObject
    {
        private const string DefaultPath = "ConfigSettings";

        [Tooltip("配置表地址前缀")]
        public string ConfigAddressPrefix = "Config";

        /// <summary>
        ///     加载默认配置
        /// </summary>
        public static ConfigSettings LoadDefault()
        {
            var settings = Resources.Load<ConfigSettings>(DefaultPath);
            if (settings == null)
            {
                settings = CreateInstance<ConfigSettings>();
                LogUtility.Warning("CFramework",
                    $"ConfigSettings not found at Resources/{DefaultPath}, using default values.");
            }

            return settings;
        }
    }
}
