#if CFRAMEWORK_AUDIO
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace CFramework
{
    /// <summary>
    ///     快照控制器 —— 自动解析 AudioMixer 的 Snapshots，支持命名切换 + 平滑过渡
    /// </summary>
    public sealed class AudioSnapshotController
    {
        private readonly AudioMixer _mixer;
        private readonly Dictionary<string, AudioMixerSnapshot> _snapshots = new();
        private string _currentSnapshot;

        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="mixer">目标 AudioMixer</param>
        /// <param name="snapshots">快照数组（从外部传入，运行时 AudioMixer 不暴露 snapshots 属性）</param>
        public AudioSnapshotController(AudioMixer mixer, AudioMixerSnapshot[] snapshots)
        {
            _mixer = mixer;
            // 从传入的快照数组构建缓存
            if (snapshots != null)
            {
                foreach (var snapshot in snapshots)
                    _snapshots[snapshot.name] = snapshot;
            }

            // 设置初始快照
            if (_snapshots.Count > 0 && snapshots != null)
                _currentSnapshot = snapshots[0].name;
        }

        /// <summary>
        ///     切换到指定快照（平滑过渡）
        /// </summary>
        /// <param name="snapshotName">快照名称</param>
        /// <param name="duration">过渡时长（秒）</param>
        public UniTask TransitionToAsync(string snapshotName, float duration = 1f)
        {
            if (!_snapshots.TryGetValue(snapshotName, out var snapshot))
            {
                Debug.LogWarning($"[Audio] Snapshot not found: {snapshotName}");
                return UniTask.CompletedTask;
            }

            _currentSnapshot = snapshotName;
            snapshot.TransitionTo(duration);
            return UniTask.Delay((int)(duration * 1000));
        }

        /// <summary>
        ///     加权混合多个快照
        /// </summary>
        public void TransitionToBlended(string[] snapshotNames, float[] weights, float duration)
        {
            var snapshots = new List<AudioMixerSnapshot>();
            foreach (var name in snapshotNames)
            {
                if (_snapshots.TryGetValue(name, out var snapshot))
                    snapshots.Add(snapshot);
                else
                    Debug.LogWarning($"[Audio] Snapshot not found in blend: {name}");
            }

            if (snapshots.Count == 0) return;

            _mixer.TransitionToSnapshots(snapshots.ToArray(), weights, duration);
        }

        /// <summary>当前快照名称</summary>
        public string CurrentSnapshot => _currentSnapshot;

        /// <summary>所有可用快照名称</summary>
        public IReadOnlyList<string> SnapshotNames => _snapshots.Keys.ToList();

        /// <summary>是否存在指定快照</summary>
        public bool HasSnapshot(string name) => _snapshots.ContainsKey(name);
    }
}
#endif
