using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     框架全局配置
    /// </summary>
    [CreateAssetMenu(fileName = "FrameworkSettings", menuName = "CFramework/Settings")]
    public sealed class FrameworkSettings : ScriptableObject
    {
        private const string DefaultPath = "FrameworkSettings";

        [Header("Asset")] [Tooltip("内存预算(MB)")]
        public int MemoryBudgetMB = 512;

        [Tooltip("每帧最大加载数量")] public int MaxLoadPerFrame = 5;

        [Header("UI")] [Tooltip("导航栈最大容量")] public int MaxNavigationStack = 10;

        [Tooltip("UIRoot Prefab 的 Addressable Key")]
        public string UIRootAddress = "UIRoot";

        [Header("Audio — 数据驱动配置")]
        [Tooltip("AudioMixer 资源地址（Addressable Key）")]
        public string AudioMixerAddress = "MasterMixer";

        [Tooltip("各分组预分配 Slot 数量（枚举名:数量，逗号分隔）\n如 Master_BGM:2,Master_SFX:5,Master_SFX_Combat:3")]
        [TextArea(2, 4)]
        public string GroupSlotConfig = "Master_BGM:2,Master_SFX:5,Master_Voice:1,Master_Ambient:1";

        [Tooltip("分组 Slot 自动扩容上限")]
        public int MaxSlotsPerGroup = 20;

        [Tooltip("音量持久化存储键前缀")]
        public string VolumePrefsPrefix = "Audio_Volume_";

        [Header("Save")] [Tooltip("自动保存间隔(秒)")]
        public int AutoSaveInterval = 60;

        [Tooltip("存档加密密钥")] public string EncryptionKey = "CFramework";

        [Header("Log")] [Tooltip("日志级别")] public LogLevel LogLevel = LogLevel.Debug;

        [Header("Config")] [Tooltip("配置表地址前缀")]
        public string ConfigAddressPrefix = "Config";

        /// <summary>
        ///     加载默认设置
        /// </summary>
        public static FrameworkSettings LoadDefault()
        {
            var settings = Resources.Load<FrameworkSettings>(DefaultPath);
            if (settings == null)
            {
                settings = CreateInstance<FrameworkSettings>();
                Debug.LogWarning(
                    $"[CFramework] FrameworkSettings not found at Resources/{DefaultPath}, using default values.");
            }

            return settings;
        }
    }
}