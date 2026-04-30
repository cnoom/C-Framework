using System;
using System.Collections.Generic;
using CFramework.Utility.Serialization;
using CNoom.UnityTool;
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
    ///     <para>注意：由于 [SerializeReference] 限制，ScriptableObject 路径仅支持引用类型 TValue</para>
    /// </summary>
    public abstract class ConfigTableAsset<TKey, TValue> : ConfigTableAsset
        where TValue : IConfigItem<TKey>
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
            {
                // SO 数据可能被多次引用，深拷贝后注入
                var copiedData = DeepCopyList(_data);
                typedTable.Load(copiedData);
            }
            else
            {
                LogUtility.Error("ConfigTableAsset",
                    $"PopulateTable 类型不匹配：期望 ConfigTable<{typeof(TKey).Name}, {typeof(TValue).Name}>，实际 {table?.GetType().Name}");
            }
        }

        /// <summary>
        ///     深拷贝数据列表
        ///     <para>如果数据行实现 ICloneable 则克隆，否则直接引用（由数据类负责不可变性）</para>
        /// </summary>
        private static List<TValue> DeepCopyList(List<TValue> source)
        {
            if (source == null) return null;

            var result = new List<TValue>(source.Count);
            foreach (var item in source)
            {
                if (item is null) continue;
                var value = item is ICloneable cloneable
                    ? (TValue)cloneable.Clone()
                    : item;
                result.Add(value);
            }

            return result;
        }
    }
}
