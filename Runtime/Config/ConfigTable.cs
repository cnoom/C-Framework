using System;
using System.Collections.Generic;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     泛型配置表（纯 C# 数据容器，运行时使用）
    ///     <para>不继承 ScriptableObject，数据通过 Load 注入</para>
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <typeparam name="TValue">数据行类型</typeparam>
    public class ConfigTable<TKey, TValue> where TValue : class, IConfigItem<TKey>
    {
        private List<TValue> _dataList;
        private Dictionary<TKey, TValue> _cache;

        /// <summary>
        ///     数据是否已加载
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        ///     数据行数
        /// </summary>
        public int Count => _dataList?.Count ?? 0;

        /// <summary>
        ///     获取所有数据行（只读）
        /// </summary>
        public IReadOnlyList<TValue> All => _dataList;

        /// <summary>
        ///     数据加载完成事件
        /// </summary>
        public event Action OnDataLoaded;

        /// <summary>
        ///     加载数据（深拷贝，避免数据污染）
        /// </summary>
        /// <param name="data">数据源</param>
        public void Load(IEnumerable<TValue> data)
        {
            _dataList = new List<TValue>();
            _cache = new Dictionary<TKey, TValue>();

            if (data != null)
            {
                foreach (var item in data)
                {
                    if (item == null) continue;

                    // 深拷贝：如果数据行实现 ICloneable 则克隆，否则直接引用
                    var value = item is ICloneable cloneable
                        ? (TValue)cloneable.Clone()
                        : item;

                    if (_cache.ContainsKey(value.Key))
                        Debug.LogWarning($"[ConfigTable] 重复主键: {value.Key}，后值覆盖前值");

                    _cache[value.Key] = value;
                    _dataList.Add(value);
                }
            }

            IsLoaded = true;
            OnDataLoaded?.Invoke();
        }

        /// <summary>
        ///     通过主键获取配置数据
        /// </summary>
        public TValue Get(TKey key)
        {
            if (_cache == null) return null;
            _cache.TryGetValue(key, out var value);
            return value;
        }

        /// <summary>
        ///     尝试获取配置数据
        /// </summary>
        public bool TryGet(TKey key, out TValue value)
        {
            if (_cache == null)
            {
                value = null;
                return false;
            }

            return _cache.TryGetValue(key, out value);
        }

        /// <summary>
        ///     是否包含指定主键
        /// </summary>
        public bool Contains(TKey key)
        {
            return _cache != null && _cache.ContainsKey(key);
        }

        /// <summary>
        ///     获取所有主键
        /// </summary>
        public IEnumerable<TKey> Keys()
        {
            if (_cache == null) yield break;
            foreach (var key in _cache.Keys) yield return key;
        }

        /// <summary>
        ///     清空数据
        /// </summary>
        public void Clear()
        {
            _dataList?.Clear();
            _cache?.Clear();
            IsLoaded = false;
        }
    }
}
