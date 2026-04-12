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

        [Header("Audio")] [Tooltip("默认BGM音量")] [Range(0f, 1f)]
        public float DefaultBGMVolume = 0.8f;

        [Tooltip("默认SFX音量")] [Range(0f, 1f)] public float DefaultSFXVolume = 1f;

        [Tooltip("默认语音音量")] [Range(0f, 1f)] public float DefaultVoiceVolume = 1f;

        [Tooltip("默认环境音量")] [Range(0f, 1f)] public float DefaultAmbientVolume = 0.5f;

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