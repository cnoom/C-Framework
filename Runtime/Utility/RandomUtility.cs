using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CFramework
{
    public static class RandomUtility
    {
        private static bool _seedSet;

        /// <summary>
        ///     从列表中随机返回一个元素
        /// </summary>
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default;
            var index = Random.Range(0, list.Count);
            return list[index];
        }

        /// <summary>
        ///     随机打乱列表顺序（Fisher-Yates）
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]); // C# 7.0+ 元组交换
            }
        }

        /// <summary>
        ///     在圆形区域内随机取点
        /// </summary>
        public static Vector2 RandomPointInCircle(float radius)
        {
            var angle = Random.Range(0f, Mathf.PI * 2);
            var r = Mathf.Sqrt(Random.Range(0f, 1f)) * radius; // 均匀分布
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        /// <summary>
        ///     根据权重选择一个元素的索引
        /// </summary>
        /// <param name="weights">权重列表，必须为正数</param>
        /// <returns>选中索引，若无效则返回-1</returns>
        public static int SelectIndex(IList<float> weights)
        {
            if (weights == null || weights.Count == 0)
                return -1;

            float total = 0;
            for (var i = 0; i < weights.Count; i++)
            {
                if (weights[i] < 0) continue;
                total += weights[i];
            }

            if (total <= 0)
                return -1;

            var random = Random.Range(0f, total);
            float accum = 0;
            for (var i = 0; i < weights.Count; i++)
            {
                if (weights[i] < 0) continue;
                accum += weights[i];
                if (random < accum)
                    return i;
            }

            return -1; // 理论上不会走到这里
        }

        /// <summary>
        ///     设置随机种子（用于复现随机序列）
        /// </summary>
        public static void SetSeed(int seed)
        {
            Random.InitState(seed);
            _seedSet = true;
        }

        /// <summary>
        ///     重置为系统时间种子
        /// </summary>
        public static void ResetToTimeSeed()
        {
            Random.InitState(Environment.TickCount);
            _seedSet = false;
        }

        /// <summary>
        ///     如果未设置过种子，则自动初始化一次（可在游戏启动时调用）
        /// </summary>
        public static void EnsureInitialized()
        {
            if (!_seedSet)
                ResetToTimeSeed();
        }
    }
}