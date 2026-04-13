using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace CFramework
{
    /// <summary>
    ///     AudioMixerGroup 路径扩展方法
    ///     <para>Unity 运行时 AudioMixerGroup 不暴露 children 属性，使用 FindMatchingGroups 替代</para>
    /// </summary>
    public static class AudioMixerGroupExtensions
    {
        /// <summary>
        ///     获取 AudioMixerGroup 的完整路径
        ///     <para>如 "Master/BGM"</para>
        /// </summary>
        public static string GetPath(this AudioMixerGroup group)
        {
            if (group == null) return null;
            var mixer = group.audioMixer;
            if (mixer == null) return group.name;

            return FindGroupPath(mixer, group);
        }

        /// <summary>
        ///     获取 AudioMixer 中所有 Group 的路径（广度优先）
        /// </summary>
        public static List<string> GetAllGroupPaths(this AudioMixer mixer)
        {
            var result = new List<string>();
            if (mixer == null) return result;

            CollectPathsBFS(mixer, "Master", result);
            return result;
        }

        /// <summary>
        ///     通过广度优先搜索查找指定分组的完整路径
        /// </summary>
        private static string FindGroupPath(AudioMixer mixer, AudioMixerGroup target)
        {
            var queue = new Queue<string>();
            queue.Enqueue("Master");

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var groups = mixer.FindMatchingGroups(path);
                if (groups.Length == 0) continue;

                // 首元素为精确匹配当前路径的分组
                if (groups[0] == target)
                    return path;

                // 将直接子分组路径加入队列
                var discovered = new HashSet<string>();
                for (int i = 1; i < groups.Length; i++)
                {
                    var name = groups[i].name;
                    if (!discovered.Contains(name))
                    {
                        discovered.Add(name);
                        queue.Enqueue(path + "/" + name);
                    }
                }
            }

            return target.name; // fallback
        }

        /// <summary>
        ///     广度优先收集所有分组路径
        /// </summary>
        private static void CollectPathsBFS(AudioMixer mixer, string rootPath, List<string> result)
        {
            var queue = new Queue<string>();
            queue.Enqueue(rootPath);

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var groups = mixer.FindMatchingGroups(path);
                if (groups.Length == 0) continue;

                result.Add(path);

                // 发现直接子分组
                var discovered = new HashSet<string>();
                for (int i = 1; i < groups.Length; i++)
                {
                    var name = groups[i].name;
                    if (!discovered.Contains(name))
                    {
                        discovered.Add(name);
                        queue.Enqueue(path + "/" + name);
                    }
                }
            }
        }
    }
}
