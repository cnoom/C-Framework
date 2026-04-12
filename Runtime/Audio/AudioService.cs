using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     音频服务实现
    /// </summary>
    public sealed class AudioService : IAudioService, IDisposable, IStartable
    {
        private readonly IAssetService _assetService;
        private readonly string[] _bgmKeys = new string[2];

        // 双音轨
        private readonly AudioSource[] _bgmTracks = new AudioSource[2];

        // 音量状态
        private readonly bool[] _groupMuted = new bool[4];
        private readonly float[] _groupVolumes = { 0.8f, 1f, 1f, 0.5f };
        private readonly FrameworkSettings _settings;
        private readonly Queue<AudioSource> _sfxPool = new();

        // SFX对象池
        private readonly GameObject _sfxRoot;

        // Ambient
        private AudioSource _ambientSource;

        private CancellationTokenSource _cts;

        // Voice
        private AudioSource _voiceSource;

        public AudioService(IAssetService assetService, FrameworkSettings settings)
        {
            _assetService = assetService;
            _settings = settings;

            _groupVolumes[(int)AudioGroup.BGM] = settings.DefaultBGMVolume;
            _groupVolumes[(int)AudioGroup.SFX] = settings.DefaultSFXVolume;
            _groupVolumes[(int)AudioGroup.Voice] = settings.DefaultVoiceVolume;
            _groupVolumes[(int)AudioGroup.Ambient] = settings.DefaultAmbientVolume;

            // 创建音频根节点
            _sfxRoot = new GameObject("[SFX]");
            Object.DontDestroyOnLoad(_sfxRoot);
        }

        public int ActiveTrack { get; private set; }

        public float BGMVolume
        {
            get => _groupVolumes[(int)AudioGroup.BGM];
            set
            {
                _groupVolumes[(int)AudioGroup.BGM] = value;
                UpdateBGMVolume();
            }
        }

        public float SFXVolume
        {
            get => _groupVolumes[(int)AudioGroup.SFX];
            set => _groupVolumes[(int)AudioGroup.SFX] = value;
        }

        public float VoiceVolume
        {
            get => _groupVolumes[(int)AudioGroup.Voice];
            set
            {
                _groupVolumes[(int)AudioGroup.Voice] = value;
                if (_voiceSource != null) _voiceSource.volume = value;
            }
        }

        public float AmbientVolume
        {
            get => _groupVolumes[(int)AudioGroup.Ambient];
            set
            {
                _groupVolumes[(int)AudioGroup.Ambient] = value;
                if (_ambientSource != null) _ambientSource.volume = value;
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            foreach (var track in _bgmTracks)
                if (track != null)
                {
                    track.Stop();
                    Object.Destroy(track.gameObject);
                }

            if (_voiceSource != null) Object.Destroy(_voiceSource.gameObject);
            if (_ambientSource != null) Object.Destroy(_ambientSource.gameObject);
            if (_sfxRoot != null) Object.Destroy(_sfxRoot);
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            // 创建BGM音轨
            for (var i = 0; i < 2; i++)
            {
                var go = new GameObject($"[BGM Track {i}]");
                Object.DontDestroyOnLoad(go);
                _bgmTracks[i] = go.AddComponent<AudioSource>();
                _bgmTracks[i].loop = true;
                _bgmTracks[i].playOnAwake = false;
            }

            // 创建Voice源
            var voiceGo = new GameObject("[Voice]");
            Object.DontDestroyOnLoad(voiceGo);
            _voiceSource = voiceGo.AddComponent<AudioSource>();
            _voiceSource.playOnAwake = false;

            // 创建Ambient源
            var ambientGo = new GameObject("[Ambient]");
            Object.DontDestroyOnLoad(ambientGo);
            _ambientSource = ambientGo.AddComponent<AudioSource>();
            _ambientSource.loop = true;
            _ambientSource.playOnAwake = false;
        }


        #region BGM

        public async UniTask PlayBGMAsync(string key, int track = 0, float fadeIn = 1f,
            CancellationToken ct = default)
        {
            if (track < 0 || track > 1) track = 0;

            var handle = await _assetService.LoadAsync<AudioClip>(key, ct);
            var clip = handle.As<AudioClip>();

            if (clip == null)
            {
                Debug.LogWarning($"[AudioService] Failed to load BGM: {key}");
                return;
            }

            _bgmKeys[track] = key;
            _bgmTracks[track].clip = clip;
            _bgmTracks[track].volume = 0f;
            _bgmTracks[track].Play();

            // 淡入
            if (fadeIn > 0)
                await FadeVolumeAsync(_bgmTracks[track], BGMVolume, fadeIn, ct);
            else
                _bgmTracks[track].volume = BGMVolume;

            ActiveTrack = track;
        }

        public void StopBGM(int track, float fadeOut = 1f)
        {
            if (track < 0 || track > 1) return;

            var source = _bgmTracks[track];

            if (fadeOut > 0 && source.isPlaying)
            {
                FadeVolumeAsync(source, 0f, fadeOut, _cts.Token)
                    .ContinueWith(() =>
                    {
                        source.Stop();
                        _bgmKeys[track] = null;
                    })
                    .Forget();
            }
            else
            {
                source.Stop();
                _bgmKeys[track] = null;
            }
        }

        public async UniTask CrossFadeAsync(string newBGM, float duration = 1f,
            CancellationToken ct = default)
        {
            var nextTrack = 1 - ActiveTrack;

            // 并行执行：新BGM淡入，旧BGM淡出
            await UniTask.WhenAll(
                PlayBGMAsync(newBGM, nextTrack, duration, ct),
                StopBGMAsync(ActiveTrack, duration, ct)
            );

            ActiveTrack = nextTrack;
        }

        private async UniTask StopBGMAsync(int track, float fadeOut, CancellationToken ct)
        {
            if (track < 0 || track > 1) return;

            var source = _bgmTracks[track];
            await FadeVolumeAsync(source, 0f, fadeOut, ct);
            source.Stop();
            _bgmKeys[track] = null;
        }

        #endregion

        #region 通道控制

        public void MuteGroup(AudioGroup group, bool mute)
        {
            _groupMuted[(int)group] = mute;

            switch (group)
            {
                case AudioGroup.BGM:
                    UpdateBGMVolume();
                    break;
                case AudioGroup.Voice:
                    if (_voiceSource != null) _voiceSource.mute = mute;
                    break;
                case AudioGroup.Ambient:
                    if (_ambientSource != null) _ambientSource.mute = mute;
                    break;
            }
        }

        public void SetGroupVolume(AudioGroup group, float volume)
        {
            _groupVolumes[(int)group] = Mathf.Clamp01(volume);

            switch (group)
            {
                case AudioGroup.BGM:
                    UpdateBGMVolume();
                    break;
                case AudioGroup.Voice:
                    if (_voiceSource != null) _voiceSource.volume = _groupVolumes[(int)group];
                    break;
                case AudioGroup.Ambient:
                    if (_ambientSource != null) _ambientSource.volume = _groupVolumes[(int)group];
                    break;
            }
        }

        private void UpdateBGMVolume()
        {
            foreach (var track in _bgmTracks)
                if (track != null)
                    track.volume = _groupMuted[(int)AudioGroup.BGM] ? 0f : BGMVolume;
        }

        #endregion

        #region SFX / Voice / Ambient

        public void PlaySFX(string key, float volume = 1f, float pitch = 1f)
        {
            PlaySFXInternal(key, Vector3.zero, volume, pitch, false);
        }

        public void PlaySFXAt(string key, Vector3 position, float volume = 1f)
        {
            PlaySFXInternal(key, position, volume, 1f, true);
        }

        private async void PlaySFXInternal(string key, Vector3 position, float volume, float pitch, bool spatial)
        {
            try
            {
                var handle = await _assetService.LoadAsync<AudioClip>(key);
                var clip = handle.As<AudioClip>();

                if (clip == null) return;

                var source = GetOrCreateSFXSource();
                source.transform.position = position;
                source.spatialBlend = spatial ? 1f : 0f;
                source.volume = volume * SFXVolume;
                source.pitch = pitch;
                source.PlayOneShot(clip);

                // 延迟回收（pitch 越大播放越快，时长越短）
                var delayMs = (int)(clip.length / pitch * 1000) + 100;
                UniTask.Delay(delayMs)
                    .ContinueWith(() => ReturnSFXSource(source))
                    .Forget();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AudioService] Failed to play SFX: {key}, Error: {ex.Message}");
            }
        }

        public async UniTask PlayVoiceAsync(string key, CancellationToken ct = default)
        {
            var handle = await _assetService.LoadAsync<AudioClip>(key, ct);
            var clip = handle.As<AudioClip>();

            if (clip == null) return;

            _voiceSource.clip = clip;
            _voiceSource.volume = VoiceVolume;
            _voiceSource.Play();

            await UniTask.WaitWhile(() => _voiceSource.isPlaying, cancellationToken: ct);
        }

        public async void PlayAmbient(string key, float fadeIn = 1f)
        {
            try
            {
                var handle = await _assetService.LoadAsync<AudioClip>(key);
                var clip = handle.As<AudioClip>();

                if (clip == null) return;

                _ambientSource.clip = clip;
                _ambientSource.volume = 0f;
                _ambientSource.Play();

                if (fadeIn > 0)
                    await FadeVolumeAsync(_ambientSource, AmbientVolume, fadeIn);
                else
                    _ambientSource.volume = AmbientVolume;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AudioService] Failed to play Ambient: {key}, Error: {ex.Message}");
            }
        }

        public void StopAmbient(float fadeOut = 1f)
        {
            if (fadeOut > 0)
                FadeVolumeAsync(_ambientSource, 0f, fadeOut)
                    .ContinueWith(() => _ambientSource.Stop())
                    .Forget();
            else
                _ambientSource.Stop();
        }

        #endregion

        #region 工具方法

        private AudioSource GetOrCreateSFXSource()
        {
            if (_sfxPool.Count > 0) return _sfxPool.Dequeue();

            var go = new GameObject("[SFX]");
            go.transform.SetParent(_sfxRoot.transform);
            return go.AddComponent<AudioSource>();
        }

        private void ReturnSFXSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            _sfxPool.Enqueue(source);
        }

        private async UniTask FadeVolumeAsync(AudioSource source, float targetVolume, float duration,
            CancellationToken ct = default)
        {
            var startVolume = source.volume;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                source.volume = Mathf.Lerp(startVolume, targetVolume, t);
                await UniTask.Yield(ct);
            }

            source.volume = targetVolume;
        }

        #endregion
    }
}