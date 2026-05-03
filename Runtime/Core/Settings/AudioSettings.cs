using UnityEngine;
using UnityEngine.Audio;

namespace CFramework
{
    /// <summary>
    ///     音频模块配置
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "CFramework/Audio Settings")]
    public sealed class AudioSettings : ScriptableObject
    {
        private const string DefaultPath = "AudioSettings";

        [Tooltip("音频混合器引用（框架内置或自定义）\n未设置时自动加载框架内置的 AudioMixer")]
        public AudioMixer AudioMixerRef;

        [Tooltip("各分组预分配 Slot 数量（枚举名:数量，逗号分隔）\n如 Master_Music:2,Master_Effect:5")]
        [TextArea(2, 4)]
        public string GroupSlotConfig = "Master_Music:2,Master_Effect:5";

        [Tooltip("分组 Slot 自动扩容上限")]
        public int MaxSlotsPerGroup = 20;

        [Tooltip("音量持久化存储键前缀")]
        public string VolumePrefsPrefix = "Audio_Volume_";

        /// <summary>
        ///     加载默认配置
        /// </summary>
        public static AudioSettings LoadDefault()
        {
            var settings = Resources.Load<AudioSettings>(DefaultPath);
            if (settings == null)
            {
                settings = CreateInstance<AudioSettings>();
                LogUtility.Warning("CFramework",
                    $"AudioSettings not found at Resources/{DefaultPath}, using default values.");
            }

            return settings;
        }
    }
}
