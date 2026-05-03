#if CFRAMEWORK_AUDIO
using System.Collections.Generic;
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
        private readonly Dictionary<int, float> _volumeCache = new(); // hash → linear volume
        private readonly Dictionary<int, bool> _muteCache = new();    // hash → muted
        private readonly Dictionary<int, string> _hashToPath = new(); // hash → path（用于持久化 key）
        private readonly string _prefsPrefix;
        private const float MinDb = -80f;

        public AudioVolumeController(AudioMixer mixer, AudioMixerTree tree, string prefsPrefix = "Audio_Volume_")
        {
            _mixer = mixer;
            _tree = tree;
            _prefsPrefix = prefsPrefix;
        }

        public void SetVolume(int hash, float linearVolume)
        {
            linearVolume = Mathf.Clamp01(linearVolume);
            _volumeCache[hash] = linearVolume;

            var paramName = ParamName(hash);
            var dbValue = _muteCache.GetValueOrDefault(hash, false) ? MinDb : LinearToDb(linearVolume);
            _mixer.SetFloat(paramName, dbValue);
        }

        public float GetVolume(int hash)
            => _volumeCache.GetValueOrDefault(hash, 1f);

        public void Mute(int hash, bool mute)
        {
            _muteCache[hash] = mute;
            SetVolume(hash, _volumeCache.GetValueOrDefault(hash, 1f));
        }

        public bool IsMuted(int hash)
            => _muteCache.GetValueOrDefault(hash, false);

        /// <summary>
        ///     验证所有分组的 Exposed Parameter 是否已配置
        /// </summary>
        public void ValidateExposedParameters(IReadOnlyList<int> hashes)
        {
            foreach (var hash in hashes)
            {
                var path = _hashToPath.TryGetValue(hash, out var p) ? p : null;
                if (path == null) continue;

                var paramName = ExposedParamName(path);
                if (!_mixer.GetFloat(paramName, out _))
                    LogUtility.Warning("Audio", $"未找到 Exposed Parameter '{paramName}'，" +
                                     $"分组 '{path}' 的音量控制将无法工作");
            }
        }

        /// <summary>
        ///     从 PlayerPrefs 加载持久化的音量设置
        /// </summary>
        public void LoadPersistentVolumes(IReadOnlyList<int> hashes)
        {
            foreach (var hash in hashes)
            {
                if (!_hashToPath.TryGetValue(hash, out var path)) continue;
                var key = $"{_prefsPrefix}{path}";
                if (PlayerPrefs.HasKey(key))
                    SetVolume(hash, PlayerPrefs.GetFloat(key));
            }
        }

        /// <summary>
        ///     将当前音量设置持久化到 PlayerPrefs
        /// </summary>
        public void SavePersistentVolumes()
        {
            foreach (var (hash, volume) in _volumeCache)
            {
                if (!_hashToPath.TryGetValue(hash, out var path)) continue;
                PlayerPrefs.SetFloat($"{_prefsPrefix}{path}", volume);
            }
            PlayerPrefs.Save();
        }

        /// <summary>注册路径→哈希映射（用于持久化 key 生成）</summary>
        public void RegisterPath(int hash, string path)
        {
            _hashToPath[hash] = path;
        }

        /// <summary>
        ///     路径 → Exposed Parameter 名称
        ///     <para>"Master/BGM" → "Master_BGM_Volume"（路径中的 "/" 替换为 "_"）</para>
        /// </summary>
        private static string ExposedParamName(string path)
            => path.Replace("/", "_") + "_Volume";

        /// <summary>哈希 → Exposed Parameter 名称（通过路径查找）</summary>
        private string ParamName(int hash)
            => _hashToPath.TryGetValue(hash, out var path) ? ExposedParamName(path) : hash.ToString();

        private static float LinearToDb(float linear)
            => linear > 0.0001f ? 20f * Mathf.Log10(linear) : MinDb;
    }
}
#endif
