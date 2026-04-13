using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     AudioMixer 解析器 + 运行时结构生成器
    ///     <para>解析 AudioMixer 的 Group 层级 → 动态生成 GameObject 结构 + AudioSource Slot</para>
    ///     <para>通过 AudioGroup 枚举（路径哈希值）O(1) 寻址</para>
    /// </summary>
    public sealed class AudioMixerTree
    {
        private readonly Dictionary<int, AudioGroupNode> _nodes = new(); // key = 枚举哈希值
        private readonly Dictionary<int, string> _pathLookup = new();    // 枚举值 → 路径字符串
        private GameObject _root;

        /// <summary>根 GameObject</summary>
        public GameObject Root => _root;

        /// <summary>
        ///     解析 AudioMixer 并生成运行时结构
        /// </summary>
        /// <param name="mixer">目标 AudioMixer</param>
        /// <param name="parent">可选父节点（为 null 则 DontDestroyOnLoad）</param>
        /// <param name="slotConfig">各分组预分配 Slot 数量，key=枚举名</param>
        /// <param name="maxSlotsPerGroup">每组最大 Slot 数</param>
        public void Build(AudioMixer mixer, Transform parent = null,
            Dictionary<string, int> slotConfig = null, int maxSlotsPerGroup = 20)
        {
            // 清理旧结构
            Dispose();

            _root = new GameObject("[Audio]");
            if (parent != null)
            {
                _root.transform.SetParent(parent);
            }
            else
            {
                Object.DontDestroyOnLoad(_root);
            }

            // 递归解析 Mixer Group 层级
            var rootGroups = mixer.FindMatchingGroups("Master");
            foreach (var group in rootGroups)
                BuildRecursive(group, _root.transform, slotConfig, maxSlotsPerGroup);
        }

        /// <summary>
        ///     通过枚举获取节点
        /// </summary>
        public AudioGroupNode GetNode(AudioGroup group)
            => _nodes.TryGetValue((int)group, out var node) ? node : null;

        /// <summary>
        ///     通过枚举获取路径字符串（内部使用）
        /// </summary>
        public string GetPath(AudioGroup group)
            => _pathLookup.TryGetValue((int)group, out var path) ? path : null;

        /// <summary>
        ///     获取所有已注册的枚举值
        /// </summary>
        public IReadOnlyList<AudioGroup> GetAllGroups()
            => _nodes.Keys.Select(h => (AudioGroup)h).ToList();

        /// <summary>
        ///     是否存在指定分组
        /// </summary>
        public bool HasGroup(AudioGroup group)
            => _nodes.ContainsKey((int)group);

        /// <summary>
        ///     销毁所有运行时结构
        /// </summary>
        public void Dispose()
        {
            foreach (var node in _nodes.Values)
                node.Dispose();

            _nodes.Clear();
            _pathLookup.Clear();

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }

        private void BuildRecursive(AudioMixerGroup group, Transform parent,
            Dictionary<string, int> slotConfig, int maxSlotsPerGroup)
        {
            var path = group.GetPath();
            var node = new AudioGroupNode(group, path, parent, maxSlotsPerGroup);
            _nodes[PathHash(path)] = node;
            _pathLookup[PathHash(path)] = path;

            // 根据配置预分配 Slot
            var enumName = path.Replace('/', '_');
            var slotCount = slotConfig != null && slotConfig.TryGetValue(enumName, out var count)
                ? count
                : 0;
            if (slotCount > 0)
                node.PreAllocateSlots(slotCount);

            // 递归处理子分组
            foreach (var child in group.children)
                BuildRecursive(child, node.Transform, slotConfig, maxSlotsPerGroup);
        }

        /// <summary>
        ///     路径字符串 → 哈希值（与编辑器代码生成器的算法一致：Animator.StringToHash）
        /// </summary>
        private static int PathHash(string path)
            => Animator.StringToHash(path);
    }
}
