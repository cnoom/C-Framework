using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace CFramework
{
    /// <summary>
    ///     AudioMixerGroup 路径扩展方法
    ///     <para>Unity 的 AudioMixerGroup 没有公开的路径属性，需要自行获取</para>
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

            return BuildPath(mixer, group);
        }

        /// <summary>
        ///     获取 AudioMixer 中所有 Group 的路径（广度优先）
        /// </summary>
        public static List<string> GetAllGroupPaths(this AudioMixer mixer)
        {
            var result = new List<string>();
            if (mixer == null) return result;

            foreach (var root in mixer.FindMatchingGroups("Master"))
                CollectPathsRecursive(root, "Master", result);

            return result;
        }

        private static string BuildPath(AudioMixer mixer, AudioMixerGroup target)
        {
            foreach (var root in mixer.FindMatchingGroups("Master"))
            {
                var result = SearchPath(root, target, "Master");
                if (result != null) return result;
            }

            return target.name; // fallback
        }

        private static string SearchPath(AudioMixerGroup current, AudioMixerGroup target, string path)
        {
            if (current == target) return path;

            foreach (var child in current.children)
            {
                var childPath = $"{path}/{child.name}";
                var result = SearchPath(child, target, childPath);
                if (result != null) return result;
            }

            return null;
        }

        private static void CollectPathsRecursive(AudioMixerGroup group, string path, List<string> result)
        {
            result.Add(path);
            foreach (var child in group.children)
            {
                var childPath = $"{path}/{child.name}";
                CollectPathsRecursive(child, childPath, result);
            }
        }
    }
}
