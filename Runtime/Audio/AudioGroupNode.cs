using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     音频分组节点 —— 对应 AudioMixer 中的一个 Group
    ///     <para>每个节点持有一个 GameObject 和多个 AudioSourceSlot</para>
    /// </summary>
    public sealed class AudioGroupNode
    {
        /// <summary>强类型枚举（路径哈希值）</summary>
        public AudioGroup Group { get; }

        /// <summary>Mixer Group 路径，如 "Master/BGM"</summary>
        public string Path { get; }

        /// <summary>对应的 AudioMixerGroup 引用</summary>
        public AudioMixerGroup MixerGroup { get; }

        /// <summary>对应的 GameObject</summary>
        public GameObject GameObject { get; }

        /// <summary>GameObject 的 Transform</summary>
        public Transform Transform { get; }

        // AudioSource Slot 池
        private readonly List<AudioSourceSlot> _slots = new();
        private readonly Queue<int> _freeSlots = new();
        private int _initialSlotCount;
        private int _maxSlots;

        // 缩容检测
        private float _lastShrinkCheck;
        private const float ShrinkInterval = 30f;

        public AudioGroupNode(AudioMixerGroup mixerGroup, string path, Transform parent, int maxSlots = 20)
        {
            MixerGroup = mixerGroup;
            Path = path;
            Group = (AudioGroup)Animator.StringToHash(path);
            _maxSlots = maxSlots;

            // 生成 GameObject：从路径提取名称 "Master/BGM" → "[BGM]"
            var name = $"[{path.Split('/').Last()}]";
            GameObject = new GameObject(name);
            GameObject.transform.SetParent(parent);
            Transform = GameObject.transform;
        }

        /// <summary>
        ///     预创建指定数量的 AudioSource Slot
        /// </summary>
        public void PreAllocateSlots(int count)
        {
            _initialSlotCount = count;
            for (int i = _slots.Count; i < count; i++)
                CreateSlot();
        }

        /// <summary>
        ///     获取或创建一个空闲 Slot
        /// </summary>
        public AudioSourceSlot AcquireSlot()
        {
            // 优先复用空闲 Slot
            if (_freeSlots.Count > 0)
            {
                var index = _freeSlots.Dequeue();
                return _slots[index];
            }

            // 没有空闲且未达上限，新建
            if (_slots.Count >= _maxSlots)
            {
                Debug.LogWarning($"[Audio] Slot pool exhausted for {Path}, max={_maxSlots}");
                // 强制复用最早的空闲（如果有的话），否则返回 null
                return null;
            }

            return CreateSlot();
        }

        /// <summary>
        ///     获取指定索引的 Slot（可能为 null）
        /// </summary>
        public AudioSourceSlot GetSlot(int index)
            => index >= 0 && index < _slots.Count ? _slots[index] : null;

        /// <summary>
        ///     获取所有 Slot（只读）
        /// </summary>
        public IReadOnlyList<AudioSourceSlot> GetAllSlots() => _slots;

        /// <summary>
        ///     释放指定 Slot 回收到池中
        /// </summary>
        public void ReleaseSlot(int index)
        {
            if (index >= 0 && index < _slots.Count && _slots[index] != null)
            {
                _slots[index].Reset();
                if (!_freeSlots.Contains(index))
                    _freeSlots.Enqueue(index);
            }
        }

        /// <summary>
        ///     释放所有 Slot
        /// </summary>
        public void ReleaseAll()
        {
            _freeSlots.Clear();
            foreach (var slot in _slots)
            {
                slot.Reset();
                _freeSlots.Enqueue(slot.Index);
            }
        }

        /// <summary>
        ///     获取当前活跃 Slot 数量
        /// </summary>
        public int ActiveSlotCount => _slots.Count(s => s != null && s.IsPlaying);

        /// <summary>
        ///     获取 Slot 总数
        /// </summary>
        public int TotalSlotCount => _slots.Count;

        /// <summary>
        ///     定时缩容：释放多余的不活跃 Slot（超出初始数量的部分）
        /// </summary>
        public void ShrinkIfNeeded()
        {
            if (Time.time - _lastShrinkCheck < ShrinkInterval) return;
            _lastShrinkCheck = Time.time;

            // 找出所有空闲 Slot 索引
            var freeSet = new HashSet<int>(_freeSlots);
            var excess = freeSet.Count - _initialSlotCount;
            if (excess <= 0) return;

            // 从高索引开始移除超出的空闲 Slot
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

        /// <summary>
        ///     销毁所有 Slot 和 GameObject
        /// </summary>
        public void Dispose()
        {
            foreach (var slot in _slots)
            {
                slot?.Reset();
            }
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
