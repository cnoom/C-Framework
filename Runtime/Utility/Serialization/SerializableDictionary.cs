using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CFramework.Utility.Serialization
{
    /// <summary>
    /// 可序列化的字典，通过 ISerializationCallbackReceiver 在 Dictionary 和 List 之间转换，
    /// 使 Unity 原生序列化器能够序列化字典类型。
    /// <para>
    /// 注意：TKey 必须是 Unity 可序列化的类型。TValue 支持具体类型和接口/抽象类型
    /// （通过 [SerializeReference] 实现多态序列化，Inspector 中自动显示子类选择器）。
    /// </para>
    /// <example>
    /// // 具体类型值
    /// [SerializeField] private SerializableDictionary&lt;string, int&gt; _intDict;
    /// // 接口类型值（Inspector 中自动显示子类下拉选择）
    /// [SerializeField] private SerializableDictionary&lt;string, IWeapon&gt; _weaponDict;
    /// </example>
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<SerializablePair> _pairs = new();

        private readonly Dictionary<TKey, TValue> _dictionary = new();

        /// <summary>
        /// 可序列化的键值对结构体
        /// </summary>
        [Serializable]
        public struct SerializablePair
        {
            public TKey Key;

            [SerializeReference, SubclassSelector]
            public TValue Value;

            public SerializablePair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        // ──────────────────────────────────────────────
        //  ISerializationCallbackReceiver
        // ──────────────────────────────────────────────

        public void OnBeforeSerialize()
        {
            if (_dictionary == null) return;

            _pairs.Clear();
            foreach (var kvp in _dictionary)
            {
                _pairs.Add(new SerializablePair(kvp.Key, kvp.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            _dictionary.Clear();

            if (_pairs == null) return;

            for (int i = 0; i < _pairs.Count; i++)
            {
                var pair = _pairs[i];
                if (pair.Key == null) continue;

                if (_dictionary.ContainsKey(pair.Key))
                {
                    LogUtility.Warning("SerializableDictionary",
                        $"反序列化时发现重复键：{pair.Key}，保留最后一个值");
                }

                _dictionary[pair.Key] = pair.Value;
            }
        }

        // ──────────────────────────────────────────────
        //  IDictionary<TKey, TValue>
        // ──────────────────────────────────────────────

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.Add(item.Key, item.Value);
        }

        public bool Remove(TKey key)
        {
            return _dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!TryGetValue(item.Key, out var value) || !Equals(value, item.Value))
                return false;

            return _dictionary.Remove(item.Key);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out var value) && Equals(value, item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < _dictionary.Count)
                throw new ArgumentException("目标数组空间不足");

            int i = arrayIndex;
            foreach (var kvp in _dictionary)
            {
                array[i++] = kvp;
            }
        }

        // ──────────────────────────────────────────────
        //  IEnumerable
        // ──────────────────────────────────────────────

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
