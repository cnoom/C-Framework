#if CFRAMEWORK_AUDIO
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     音频服务实现 —— 数据驱动，基于 AudioMixer 动态解析
    ///     <para>核心职责：协调 AudioMixerTree / AudioVolumeController / AudioPlaybackController / AudioSnapshotController</para>
    ///     <para>使用 FrameworkSettings 中指定的 AudioMixer 自动初始化</para>
    ///     <para>分组寻址通过字符串路径（如 "Master/BGM"），与用户生成的 AudioGroup 枚举完全解耦</para>
    /// </summary>
    public sealed class AudioService : IAudioService, IStartable
    {
        private readonly IAssetService _assetService;
        private readonly FrameworkSettings _settings;

        private AudioMixerTree _tree;
        private AudioVolumeController _volumeCtrl;
        private AudioPlaybackController _playbackCtrl;
        private AudioSnapshotController _snapshotCtrl;

        private AudioMixer _mixer;
        private bool _initialized;
        private List<(string path, int slotIndex)> _pausedSlots = new();

        public AudioService(IAssetService assetService, FrameworkSettings settings)
        {
            _assetService = assetService;
            _settings = settings;
        }

        #region IStartable

        public void Start()
        {
            if (!_initialized)
                SafeInitializeAsync().Forget();
        }

        /// <summary>
        ///     安全初始化包装，捕获异步异常并输出日志
        /// </summary>
        private async UniTaskVoid SafeInitializeAsync()
        {
            try
            {
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Audio] Initialization failed: {ex.Message}");
            }
        }

        #endregion

        #region 初始化

        public UniTask InitializeAsync()
        {
            if (_initialized)
            {
                Debug.LogWarning("[Audio] Already initialized, disposing old resources first.");
                DisposeInternal();
            }

            _mixer = _settings.AudioMixerRef;
            if (_mixer == null)
            {
                Debug.LogError("[Audio] AudioMixerRef is null in FrameworkSettings. " +
                               "Please assign an AudioMixer in FrameworkSettings.");
                return UniTask.CompletedTask;
            }

            return InitializeInternalAsync(null);
        }

        public UniTask InitializeAsync(AudioMixer mixer, AudioMixerSnapshot[] snapshots = null)
        {
            if (_initialized)
            {
                Debug.LogWarning("[Audio] Already initialized, disposing old resources first.");
                DisposeInternal();
            }

            _mixer = mixer;
            if (_mixer == null)
            {
                Debug.LogError("[Audio] AudioMixer is null.");
                return UniTask.CompletedTask;
            }

            return InitializeInternalAsync(snapshots);
        }

        private UniTask InitializeInternalAsync(AudioMixerSnapshot[] snapshots)
        {
            _tree = new AudioMixerTree();
            var slotConfig = ParseSlotConfig(_settings.GroupSlotConfig);
            _tree.Build(_mixer, slotConfig: slotConfig, maxSlotsPerGroup: _settings.MaxSlotsPerGroup);

            _volumeCtrl = new AudioVolumeController(_mixer, _tree, _settings.VolumePrefsPrefix);
            // 注册所有路径→哈希映射（用于 Exposed Parameter 名称推导和 PlayerPrefs key 生成）
            foreach (var (path, hash) in _tree.GetAllPathToHash())
                _volumeCtrl.RegisterPath(hash, path);
            _volumeCtrl.ValidateExposedParameters(_tree.GetAllHashes());
            _volumeCtrl.LoadPersistentVolumes(_tree.GetAllHashes());

            _playbackCtrl = new AudioPlaybackController(_tree, _assetService);
            _snapshotCtrl = new AudioSnapshotController(_mixer, snapshots);

            _initialized = true;
            Debug.Log($"[Audio] Initialized. Groups: {_tree.GetAllPaths().Count}, " +
                      $"Snapshots: {_snapshotCtrl.SnapshotNames.Count}");
            return UniTask.CompletedTask;
        }

        #endregion

        #region 音量控制

        public void SetGroupVolume(string groupPath, float volume)
        {
            ThrowIfNotInitialized();
            var hash = PathHash(groupPath);
            _volumeCtrl.SetVolume(hash, volume);
        }

        public float GetGroupVolume(string groupPath)
        {
            ThrowIfNotInitialized();
            return _volumeCtrl.GetVolume(PathHash(groupPath));
        }

        public void MuteGroup(string groupPath, bool mute)
        {
            ThrowIfNotInitialized();
            _volumeCtrl.Mute(PathHash(groupPath), mute);
        }

        public bool IsGroupMuted(string groupPath)
        {
            ThrowIfNotInitialized();
            return _volumeCtrl.IsMuted(PathHash(groupPath));
        }

        #endregion

        #region 快照系统

        public UniTask TransitionToSnapshotAsync(string snapshotName, float duration = 1f)
        {
            ThrowIfNotInitialized();
            return _snapshotCtrl.TransitionToAsync(snapshotName, duration);
        }

        public string CurrentSnapshot
        {
            get
            {
                ThrowIfNotInitialized();
                return _snapshotCtrl.CurrentSnapshot;
            }
        }

        public IReadOnlyList<string> GetSnapshotNames()
        {
            ThrowIfNotInitialized();
            return _snapshotCtrl.SnapshotNames;
        }

        #endregion

        #region 播放控制

        public UniTask<AudioSourceSlot> PlayAsync(string groupPath, string clipKey,
            AudioPlayOptions options = default, CancellationToken ct = default)
        {
            ThrowIfNotInitialized();
            return _playbackCtrl.PlayAsync(groupPath, clipKey, options, ct);
        }

        public void Stop(string groupPath, int slotIndex = -1, float fadeOut = 0f)
        {
            ThrowIfNotInitialized();
            if (slotIndex < 0)
                _playbackCtrl.StopLast(groupPath, fadeOut);
            else
                _playbackCtrl.Stop(groupPath, slotIndex, fadeOut);
        }

        public void StopAll(string groupPath, float fadeOut = 0f)
        {
            ThrowIfNotInitialized();
            _playbackCtrl.StopAll(groupPath, fadeOut);
        }

        public UniTask CrossFadeAsync(string groupPath, string newClipKey,
            float duration = 1f, AudioPlayOptions options = default,
            CancellationToken ct = default)
        {
            ThrowIfNotInitialized();
            return _playbackCtrl.CrossFadeAsync(groupPath, newClipKey, duration, options, ct);
        }

        #endregion

        #region 暂停/恢复

        public void PauseAll()
        {
            ThrowIfNotInitialized();
            _pausedSlots = _playbackCtrl.PauseAll();
        }

        public void ResumeAll()
        {
            ThrowIfNotInitialized();
            _playbackCtrl.ResumeSlots(_pausedSlots);
            _pausedSlots.Clear();
        }

        #endregion

        #region 查询

        public IReadOnlyList<string> GetAllGroupPaths()
        {
            ThrowIfNotInitialized();
            return _tree.GetAllPaths();
        }

        public bool HasGroup(string groupPath)
        {
            ThrowIfNotInitialized();
            return _tree.HasPath(groupPath);
        }

        public AudioGroupInfo GetGroupInfo(string groupPath)
        {
            ThrowIfNotInitialized();
            var hash = PathHash(groupPath);
            var node = _tree.GetNode(hash);
            if (node == null)
                return default;

            return new AudioGroupInfo
            {
                Path = groupPath,
                TotalSlots = node.TotalSlotCount,
                ActiveSlots = node.ActiveSlotCount,
                Volume = _volumeCtrl.GetVolume(hash),
                IsMuted = _volumeCtrl.IsMuted(hash)
            };
        }

        #endregion

        #region 持久化

        public void SaveVolumes()
        {
            ThrowIfNotInitialized();
            _volumeCtrl.SavePersistentVolumes();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_initialized && _volumeCtrl != null)
                _volumeCtrl.SavePersistentVolumes();
            DisposeInternal();
        }

        private void DisposeInternal()
        {
            _tree?.Dispose();
            _tree = null;
            _volumeCtrl = null;
            _playbackCtrl = null;
            _snapshotCtrl = null;
            _mixer = null;
            _pausedSlots.Clear();
            _initialized = false;
        }

        #endregion

        #region 内部工具

        private void ThrowIfNotInitialized()
        {
            if (!_initialized)
                throw new System.InvalidOperationException(
                    "[Audio] AudioService has not been initialized. Call InitializeAsync() first.");
        }

        /// <summary>路径字符串 → 哈希值（与编辑器代码生成器一致：Animator.StringToHash）</summary>
        private static int PathHash(string path) => Animator.StringToHash(path);

        private static Dictionary<string, int> ParseSlotConfig(string config)
        {
            var result = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(config)) return result;
            foreach (var entry in config.Split(','))
            {
                var parts = entry.Trim().Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var count))
                    result[parts[0].Trim()] = count;
            }
            return result;
        }

        #endregion
    }
}
#endif
