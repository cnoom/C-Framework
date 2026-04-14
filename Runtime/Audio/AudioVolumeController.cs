#if CFRAMEWORK_AUDIO
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace CFramework
{
    /// <summary>
    ///     音量控制器 —— 通过 AudioMixer Exposed Parameters 控制各 Group 音量
    ///     <para>所有音量操作都在 dB 域，对外暴露 0~1 线性值</para>
    /// </summary>
    public sealed class AudioVolumeController
    {
        private readonly AudioMixer _mixer;
        private readonly AudioMixerTree _tree;
        private readonly Dictionary<int, float> _volumeCache = new();  // 枚举哈希 → 线性值
        private readonly Dictionary<int, bool> _muteCache = new();     // 枚举哈希 → 是否静音
        private readonly string _prefsPrefix;

        private const float MinDb = -80f;

        public AudioVolumeController(AudioMixer mixer, AudioMixerTree tree, string prefsPrefix = "Audio_Volume_")
        {
            _mixer = mixer;
            _tree = tree;
            _prefsPrefix = prefsPrefix;
        }

        /// <summary>
        ///     设置分组音量（传入 0~1 线性值，内部转 dB）
        /// </summary>
        public void SetVolume(AudioGroup group, float linearVolume)
        {
            linearVolume = Mathf.Clamp01(linearVolume);
            _volumeCache[(int)group] = linearVolume;

            var paramName = ToExposedParamName(group);
            var dbValue = _muteCache.GetValueOrDefault((int)group, false)
                ? MinDb
                : LinearToDb(linearVolume);
            _mixer.SetFloat(paramName, dbValue);
        }

        /// <summary>
        ///     获取分组音量（返回 0~1 线性值）
        /// </summary>
        public float GetVolume(AudioGroup group)
            => _volumeCache.GetValueOrDefault((int)group, 1f);

        /// <summary>
        ///     静音/取消静音
        /// </summary>
        public void Mute(AudioGroup group, bool mute)
        {
            _muteCache[(int)group] = mute;
            // 重新应用音量（静音时设为 MinDb，取消静音时恢复缓存值）
            SetVolume(group, _volumeCache.GetValueOrDefault((int)group, 1f));
        }

        /// <summary>
        ///     是否已静音
        /// </summary>
        public bool IsMuted(AudioGroup group)
            => _muteCache.GetValueOrDefault((int)group, false);

        /// <summary>
        ///     验证所有分组的 Exposed Parameter 是否已配置
        ///     <para>未配置的分组音量控制将不生效，输出警告日志</para>
        /// </summary>
        public void ValidateExposedParameters(IEnumerable<AudioGroup> groups)
        {
            foreach (var group in groups)
            {
                var paramName = ToExposedParamName(group);
                if (!_mixer.GetFloat(paramName, out _))
                {
                    Debug.LogWarning($"[Audio] Exposed Parameter '{paramName}' not found. " +
                                     $"Group '{group}' volume control will not work.");
                }
            }
        }

        /// <summary>
        ///     从 PlayerPrefs 加载持久化的音量设置
        /// </summary>
        public void LoadPersistentVolumes(IEnumerable<AudioGroup> groups)
        {
            foreach (var group in groups)
            {
                var key = $"{_prefsPrefix}{group}";
                if (PlayerPrefs.HasKey(key))
                {
                    var savedVolume = PlayerPrefs.GetFloat(key);
                    SetVolume(group, savedVolume);
                }
            }
        }

        /// <summary>
        ///     将当前音量设置持久化到 PlayerPrefs
        /// </summary>
        public void SavePersistentVolumes()
        {
            foreach (var (groupHash, volume) in _volumeCache)
            {
                var group = (AudioGroup)groupHash;
                var key = $"{_prefsPrefix}{group}";
                PlayerPrefs.SetFloat(key, volume);
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        ///     枚举 → Exposed Parameter 名称
        ///     <para>AudioGroup.Master_BGM → "Master_BGM_Volume"</para>
        /// </summary>
        private string ToExposedParamName(AudioGroup group)
            => group.ToString() + "_Volume";

        /// <summary>
        ///     线性值 → dB 值
        /// </summary>
        private static float LinearToDb(float linear)
            => linear > 0.0001f ? 20f * Mathf.Log10(linear) : MinDb;
    }
}
#endif
