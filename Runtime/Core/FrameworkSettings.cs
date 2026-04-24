using UnityEngine;
using UnityEngine.Audio;

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

        [Header("Audio")]
        [Tooltip("音频混合器引用（框架内置或自定义）\n未设置时自动加载框架内置的 AudioMixer（Prefabs/AudioMixer.mixer）")]
        public AudioMixer AudioMixerRef;

        [Tooltip("各分组预分配 Slot 数量（枚举名:数量，逗号分隔）\n如 Master_Music:2,Master_Effect:5")]
        [TextArea(2, 4)]
        public string GroupSlotConfig = "Master_Music:2,Master_Effect:5";

        [Tooltip("分组 Slot 自动扩容上限")]
        public int MaxSlotsPerGroup = 20;

        [Tooltip("音量持久化存储键前缀")]
        public string VolumePrefsPrefix = "Audio_Volume_";

        [Header("Save")] [Tooltip("自动保存间隔(秒)")]
        public int AutoSaveInterval = 60;

        [Tooltip("存档加密密钥（AES-128 需要 16 字符，AES-256 需要 32 字符）\n留空则不加密，以明文存储")]
        public string EncryptionKey = "";

        [Header("Pool")]
        [Tooltip("对象池默认初始容量")]
        public int PoolDefaultCapacity = 10;

        [Tooltip("对象池默认最大容量（0=不限）")]
        public int PoolMaxSize = 100;

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