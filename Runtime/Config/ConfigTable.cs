using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace CFramework
{
    /// <summary>
    ///     泛型配置表
    /// </summary>
    public abstract class ConfigTable<TKey, TValue> : ConfigTableBase where TValue : class, IConfigItem<TKey>
    {
        /// <summary>
        ///     配置数据列表
        /// </summary>
#if ODIN_INSPECTOR
        [OdinSerialize]
#else
        [SerializeField]
#endif
        [TableList]
        [ShowInInspector]
        [PropertyOrder(1)]
        [Searchable]
        protected List<TValue> dataList = new();

        /// <summary>
        ///     字典缓存，用于快速查找
        /// </summary>
        [NonSerialized] private Dictionary<TKey, TValue> _cache;

        public override int Count => dataList.Count;

        /// <summary>
        ///     获取配置数据列表
        /// </summary>
        public IReadOnlyList<TValue> DataList => dataList;

        /// <summary>
        ///     通过主键获取配置数据
        /// </summary>
        public TValue Get(TKey key)
        {
            EnsureCache();
            _cache.TryGetValue(key, out var value);
            return value;
        }

        /// <summary>
        ///     尝试获取配置数据
        /// </summary>
        public bool TryGet(TKey key, out TValue value)
        {
            EnsureCache();
            return _cache.TryGetValue(key, out value);
        }

        /// <summary>
        ///     获取所有主键
        /// </summary>
        public IEnumerable<TKey> Keys()
        {
            EnsureCache();
            return _cache.Keys;
        }

        /// <summary>
        ///     扩展点：从外部注入数据
        /// </summary>
        public void SetData(List<TValue> newData, ConfigDataSource source = ConfigDataSource.External)
        {
            dataList = newData ?? throw new ArgumentNullException(nameof(newData));
            _cache = null; // 清除缓存，下次访问时重建
            Source = source;
            IsLoaded = true;
            NotifyDataLoaded();
        }

        /// <summary>
        ///     确保字典缓存已初始化
        /// </summary>
        private void EnsureCache()
        {
            if (_cache != null) return;

            _cache = new Dictionary<TKey, TValue>();
            foreach (var item in dataList)
                if (item != null)
                    _cache[item.Key] = item;
        }

        /// <summary>
        ///     清除缓存（在数据变更后调用）
        /// </summary>
        protected void InvalidateCache()
        {
            _cache = null;
        }
    }
}