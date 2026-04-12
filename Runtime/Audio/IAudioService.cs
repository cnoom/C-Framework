using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     音频服务接口
    /// </summary>
    public interface IAudioService
    {
        #region BGM双音轨系统

        /// <summary>
        ///     指定音轨播放BGM
        /// </summary>
        UniTask PlayBGMAsync(string key, int track = 0, float fadeIn = 1f, CancellationToken ct = default);

        /// <summary>
        ///     停止指定音轨
        /// </summary>
        void StopBGM(int track, float fadeOut = 1f);

        /// <summary>
        ///     交叉淡入淡出到另一音轨
        /// </summary>
        UniTask CrossFadeAsync(string newBGM, float duration = 1f, CancellationToken ct = default);

        /// <summary>
        ///     当前活跃音轨
        /// </summary>
        int ActiveTrack { get; }

        #endregion

        #region 通道控制

        float BGMVolume { get; set; }
        float SFXVolume { get; set; }
        float VoiceVolume { get; set; }
        float AmbientVolume { get; set; }

        void MuteGroup(AudioGroup group, bool mute);
        void SetGroupVolume(AudioGroup group, float volume);

        #endregion

        #region SFX / Voice / Ambient

        void PlaySFX(string key, float volume = 1f, float pitch = 1f);
        void PlaySFXAt(string key, Vector3 position, float volume = 1f);
        UniTask PlayVoiceAsync(string key, CancellationToken ct = default);
        void PlayAmbient(string key, float fadeIn = 1f);
        void StopAmbient(float fadeOut = 1f);

        #endregion
    }
}