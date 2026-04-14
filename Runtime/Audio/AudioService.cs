#if CFRAMEWORK_AUDIO
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

        // 全局暂停状态
        private List<(AudioGroup group, int slotIndex)> _pausedSlots = new();

        public AudioService(IAssetService assetService, FrameworkSettings settings)
        {
            _assetService = assetService;
            _settings = settings;
        }

        #region IStartable

        public void Start()
        {
            // IStartable 由 VContainer 自动调用
            // 初始化延迟到 InitializeAsync 被显式调用
        }

        #endregion

        #region 初始化

        public UniTask InitializeAsync(AudioMixer mixer, AudioMixerSnapshot[] snapshots = null)
        {
            if (_initialized)
            {
                Debug.LogWarning("[Audio] Already initialized, disposing old resources first.");
                DisposeInternal();
            }

            _mixer = mixer;

            // 1. 解析 Mixer → 生成 GameObject + Slot
            _tree = new AudioMixerTree();
            var slotConfig = ParseSlotConfig(_settings.GroupSlotConfig);
            _tree.Build(mixer, slotConfig: slotConfig, maxSlotsPerGroup: _settings.MaxSlotsPerGroup);

            // 2. 音量控制器
            _volumeCtrl = new AudioVolumeController(mixer, _tree, _settings.VolumePrefsPrefix);

            // 3. 验证 Exposed Parameters
            _volumeCtrl.ValidateExposedParameters(_tree.GetAllGroups());

            // 4. 加载持久化音量
            _volumeCtrl.LoadPersistentVolumes(_tree.GetAllGroups());

            // 5. 播放控制器
            _playbackCtrl = new AudioPlaybackController(_tree, _assetService);

            // 6. 快照控制器
            _snapshotCtrl = new AudioSnapshotController(mixer, snapshots);

            _initialized = true;
            Debug.Log($"[Audio] Initialized. Groups: {_tree.GetAllGroups().Count}, " +
                      $"Snapshots: {_snapshotCtrl.SnapshotNames.Count}");

            return UniTask.CompletedTask;
        }

        #endregion

        #region 音量控制

        public void SetGroupVolume(AudioGroup group, float volume)
        {
            ThrowIfNotInitialized();
            _volumeCtrl.SetVolume(group, volume);
        }

        public float GetGroupVolume(AudioGroup group)
        {
            ThrowIfNotInitialized();
            return _volumeCtrl.GetVolume(group);
        }

        public void MuteGroup(AudioGroup group, bool mute)
        {
            ThrowIfNotInitialized();
            _volumeCtrl.Mute(group, mute);
        }

        public bool IsGroupMuted(AudioGroup group)
        {
            ThrowIfNotInitialized();
            return _volumeCtrl.IsMuted(group);
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

        public UniTask<AudioSourceSlot> PlayAsync(AudioGroup group, string clipKey,
            AudioPlayOptions options = default, CancellationToken ct = default)
        {
            ThrowIfNotInitialized();
            return _playbackCtrl.PlayAsync(group, clipKey, options, ct);
        }

        public void Stop(AudioGroup group, int slotIndex = -1, float fadeOut = 0f)
        {
            ThrowIfNotInitialized();
            if (slotIndex < 0)
                _playbackCtrl.StopLast(group, fadeOut);
            else
                _playbackCtrl.Stop(group, slotIndex, fadeOut);
        }

        public void StopAll(AudioGroup group, float fadeOut = 0f)
        {
            ThrowIfNotInitialized();
            _playbackCtrl.StopAll(group, fadeOut);
        }

        public UniTask CrossFadeAsync(AudioGroup group, string newClipKey,
            float duration = 1f, AudioPlayOptions options = default,
            CancellationToken ct = default)
        {
            ThrowIfNotInitialized();
            return _playbackCtrl.CrossFadeAsync(group, newClipKey, duration, options, ct);
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

        public IReadOnlyList<AudioGroup> GetAllGroups()
        {
            ThrowIfNotInitialized();
            return _tree.GetAllGroups();
        }

        public bool HasGroup(AudioGroup group)
        {
            ThrowIfNotInitialized();
            return _tree.HasGroup(group);
        }

        public AudioGroupInfo GetGroupInfo(AudioGroup group)
        {
            ThrowIfNotInitialized();
            var node = _tree.GetNode(group);
            if (node == null)
                return default;

            return new AudioGroupInfo
            {
                Group = group,
                Path = node.Path,
                TotalSlots = node.TotalSlotCount,
                ActiveSlots = node.ActiveSlotCount,
                Volume = _volumeCtrl.GetVolume(group),
                IsMuted = _volumeCtrl.IsMuted(group)
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
            // 保存音量设置
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

        /// <summary>
        ///     解析 Slot 配置字符串
        ///     <para>格式："Master_BGM:2,Master_SFX:5,Master_SFX_Combat:3"</para>
        /// </summary>
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
