#if CFRAMEWORK_AUDIO
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     音频分组节点 —— 对应 AudioMixer 中的一个 Group
    ///     <para>每个节点持有一个 GameObject 和多个 AudioSourceSlot</para>
    /// </summary>
    public sealed class AudioGroupNode
    {
        /// <summary>Mixer Group 路径，如 "Master/BGM"</summary>
        public string Path { get; }

        /// <summary>对应的 AudioMixerGroup 引用</summary>
        public AudioMixerGroup MixerGroup { get; }

        /// <summary>对应的 GameObject</summary>
        public GameObject GameObject { get; }

        /// <summary>GameObject 的 Transform</summary>
        public Transform Transform { get; }

        private readonly List<AudioSourceSlot> _slots = new();
        private readonly Queue<int> _freeSlots = new();
        private int _initialSlotCount;
        private int _maxSlots;
        private float _lastShrinkCheck;
        private const float ShrinkInterval = 30f;

        public AudioGroupNode(AudioMixerGroup mixerGroup, string path, Transform parent, int maxSlots = 20)
        {
            MixerGroup = mixerGroup;
            Path = path;
            _maxSlots = maxSlots;

            var name = $"[{path.Split('/').Last()}]";
            GameObject = new GameObject(name);
            GameObject.transform.SetParent(parent);
            Transform = GameObject.transform;
        }

        public void PreAllocateSlots(int count)
        {
            _initialSlotCount = count;
            for (int i = _slots.Count; i < count; i++)
                CreateSlot();
        }

        public AudioSourceSlot AcquireSlot()
        {
            if (_freeSlots.Count > 0)
            {
                var index = _freeSlots.Dequeue();
                return _slots[index];
            }

            if (_slots.Count >= _maxSlots)
            {
                Debug.LogWarning($"[Audio] Slot pool exhausted for {Path}, max={_maxSlots}");
                return null;
            }

            return CreateSlot();
        }

        public AudioSourceSlot GetSlot(int index)
            => index >= 0 && index < _slots.Count ? _slots[index] : null;

        public IReadOnlyList<AudioSourceSlot> GetAllSlots() => _slots;

        public void ReleaseSlot(int index)
        {
            if (index >= 0 && index < _slots.Count && _slots[index] != null)
            {
                _slots[index].Reset();
                if (!_freeSlots.Contains(index))
                    _freeSlots.Enqueue(index);
            }
        }

        public void ReleaseAll()
        {
            _freeSlots.Clear();
            foreach (var slot in _slots)
            {
                slot.Reset();
                _freeSlots.Enqueue(slot.Index);
            }
        }

        public int ActiveSlotCount => _slots.Count(s => s != null && s.IsPlaying);

        public int TotalSlotCount => _slots.Count;

        public void ShrinkIfNeeded()
        {
            if (Time.time - _lastShrinkCheck < ShrinkInterval) return;
            _lastShrinkCheck = Time.time;

            var freeSet = new HashSet<int>(_freeSlots);
            var excess = freeSet.Count - _initialSlotCount;
            if (excess <= 0) return;

            var toRemove = freeSet.OrderByDescending(x => x).Take(excess).ToList();
            foreach (var idx in toRemove)
            {
                if (idx >= _initialSlotCount && _slots[idx] != null)
                {
                    Object.Destroy(_slots[idx].Source);
                    _slots[idx] = null;
                }
            }

            _slots.RemoveAll(s => s == null);
            RebuildFreeQueue();
        }

        public void Dispose()
        {
            foreach (var slot in _slots)
                slot?.Reset();
            _slots.Clear();
            _freeSlots.Clear();
            if (GameObject != null)
                Object.Destroy(GameObject);
        }

        private AudioSourceSlot CreateSlot()
        {
            var index = _slots.Count;
            var source = GameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = MixerGroup;

            var slot = new AudioSourceSlot(index, source);
            _slots.Add(slot);
            _freeSlots.Enqueue(index);
            return slot;
        }

        private void RebuildFreeQueue()
        {
            _freeSlots.Clear();
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && !_slots[i].IsPlaying)
                    _freeSlots.Enqueue(i);
            }
        }
    }
}
#endif
