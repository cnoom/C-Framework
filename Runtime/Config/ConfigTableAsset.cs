using System;
using System.Collections.Generic;
using CFramework.Utility.Serialization;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     配置表资产基类（编辑器专用 ScriptableObject）
    ///     <para>作为数据编辑载体，运行时通过 PopulateTable 将数据注入纯 C# 的 ConfigTable</para>
    ///     <para>用户子类化此类型以在 Inspector 中编辑配置数据</para>
    /// </summary>
    public abstract class ConfigTableAsset : ScriptableObject
    {
        /// <summary>
        ///     主键类型
        /// </summary>
        public abstract Type KeyType { get; }

        /// <summary>
        ///     数据行类型
        /// </summary>
        public abstract Type ValueType { get; }

        /// <summary>
        ///     数据行数
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        ///     配置表显示名称
        /// </summary>
        public abstract string TableName { get; }

        /// <summary>
        ///     将数据填充到运行时 ConfigTable 中（深拷贝）
        /// </summary>
        /// <param name="table">目标 ConfigTable 实例</param>
        public abstract void PopulateTable(object table);
    }

    /// <summary>
    ///     泛型配置表资产
    ///     <para>持有序列化数据列表，运行时通过 PopulateTable 注入 ConfigTable</para>
    /// </summary>
    public abstract class ConfigTableAsset<TKey, TValue> : ConfigTableAsset
        where TValue : class, IConfigItem<TKey>
    {
        [Header("配置数据")]
        [Tooltip("配置数据行列表")]
        [SerializeReference]
        [SubclassSelector]
        private List<TValue> _data = new();

        public override Type KeyType => typeof(TKey);
        public override Type ValueType => typeof(TValue);
        public override int Count => _data?.Count ?? 0;
        public override string TableName => GetType().Name;

        /// <summary>
        ///     获取数据列表（编辑器用）
        /// </summary>
        public List<TValue> Data => _data;

        public override void PopulateTable(object table)
        {
            if (table is ConfigTable<TKey, TValue> typedTable)
                typedTable.Load(_data);
            else
                Debug.LogError($"[ConfigTableAsset] PopulateTable 类型不匹配：期望 ConfigTable<{typeof(TKey).Name}, {typeof(TValue).Name}>，实际 {table?.GetType().Name}");
        }
    }
}
