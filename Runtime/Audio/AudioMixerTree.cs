#if CFRAMEWORK_AUDIO
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     AudioMixer 解析器 + 运行时结构生成器
    ///     <para>解析 AudioMixer 的 Group 层级 → 动态生成 GameObject 结构 + AudioSource Slot</para>
    ///     <para>内部通过路径哈希值 O(1) 寻址，外部通过字符串路径操作</para>
    /// </summary>
    public sealed class AudioMixerTree
    {
        private readonly Dictionary<int, AudioGroupNode> _nodes = new();  // hash → node
        private readonly Dictionary<string, int> _pathToHash = new();     // path → hash
        private GameObject _root;

        public GameObject Root => _root;

        public void Build(AudioMixer mixer, Transform parent = null,
            Dictionary<string, int> slotConfig = null, int maxSlotsPerGroup = 20)
        {
            Dispose();

            _root = new GameObject("[Audio]");
            if (parent != null)
                _root.transform.SetParent(parent);
            else
                Object.DontDestroyOnLoad(_root);

            var rootGroups = mixer.FindMatchingGroups("Master");
            if (rootGroups.Length > 0)
                BuildRecursive(mixer, rootGroups[0], "Master", _root.transform, slotConfig, maxSlotsPerGroup);
        }

        /// <summary>通过路径字符串获取节点（大小写敏感）</summary>
        public AudioGroupNode GetNode(string path)
            => _nodes.TryGetValue(PathHash(path), out var node) ? node : null;

        /// <summary>通过哈希值获取节点</summary>
        public AudioGroupNode GetNode(int hash)
            => _nodes.TryGetValue(hash, out var node) ? node : null;

        /// <summary>获取所有已注册的 Group 路径</summary>
        public IReadOnlyList<string> GetAllPaths()
        {
            var paths = new List<string>(_pathToHash.Count);
            foreach (var p in _pathToHash.Keys)
                paths.Add(p);
            return paths;
        }

        /// <summary>获取所有已注册的路径哈希值</summary>
        public IReadOnlyList<int> GetAllHashes()
        {
            var hashes = new List<int>(_nodes.Count);
            foreach (var h in _nodes.Keys)
                hashes.Add(h);
            return hashes;
        }

        /// <summary>获取所有路径→哈希的映射（供 AudioVolumeController 注册用）</summary>
        public IReadOnlyDictionary<string, int> GetAllPathToHash()
            => _pathToHash;

        /// <summary>是否存在指定路径的 Group</summary>
        public bool HasPath(string path) => _pathToHash.ContainsKey(path);

        public void Dispose()
        {
            foreach (var node in _nodes.Values)
                node.Dispose();
            _nodes.Clear();
            _pathToHash.Clear();
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }

        private void BuildRecursive(AudioMixer mixer, AudioMixerGroup group, string path,
            Transform parent, Dictionary<string, int> slotConfig, int maxSlotsPerGroup)
        {
            var hash = PathHash(path);
            var node = new AudioGroupNode(group, path, parent, maxSlotsPerGroup);
            _nodes[hash] = node;
            _pathToHash[path] = hash;

            // 根据路径预分配 Slot（路径 "/" 替换为 "_" 匹配配置格式）
            var configKey = path.Replace("/", "_");
            var slotCount = slotConfig != null && slotConfig.TryGetValue(configKey, out var count)
                ? count : 0;
            if (slotCount > 0)
                node.PreAllocateSlots(slotCount);

            // 递归子分组
            var allUnder = mixer.FindMatchingGroups(path);
            var discovered = new HashSet<string>();
            for (int i = 1; i < allUnder.Length; i++)
            {
                var childName = allUnder[i].name;
                if (discovered.Contains(childName)) continue;
                var childPath = path + "/" + childName;
                var childGroups = mixer.FindMatchingGroups(childPath);
                if (childGroups.Length > 0)
                {
                    discovered.Add(childName);
                    BuildRecursive(mixer, childGroups[0], childPath, node.Transform, slotConfig, maxSlotsPerGroup);
                }
            }
        }

        private static int PathHash(string path) => Animator.StringToHash(path);
    }
}
#endif
