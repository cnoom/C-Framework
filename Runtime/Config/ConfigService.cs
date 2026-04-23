using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace CFramework
{
    /// <summary>
    ///     配置服务实现
    ///     <para>通过 IConfigProvider 加载配置数据，管理 ConfigTable 实例的生命周期</para>
    ///     <para>支持多 Provider：通过 CompositeConfigProvider 按地址前缀自动路由</para>
    /// </summary>
    public sealed class ConfigService : IConfigService
    {
        private readonly IConfigProvider _provider;
        private readonly FrameworkSettings _settings;

        /// <summary>
        ///     数据类型 → ConfigTable 实例（object 装箱，因为 TKey 编译期未知）
        /// </summary>
        private readonly Dictionary<Type, object> _tables = new();

        /// <summary>
        ///     数据类型 → 加载地址
        /// </summary>
        private readonly Dictionary<Type, string> _addresses = new();

        private bool _disposed;

        public ConfigService(IConfigProvider provider, FrameworkSettings settings)
        {
            _provider = provider;
            _settings = settings;
        }

        public async UniTask LoadAsync<TValue>(string address = null, CancellationToken ct = default)
            where TValue : IConfigItem
        {
            ct.ThrowIfCancellationRequested();

            var valueType = typeof(TValue);
            if (_tables.ContainsKey(valueType)) return;

            // 解析地址
            if (string.IsNullOrEmpty(address))
            {
                if (!_addresses.TryGetValue(valueType, out address))
                {
                    // 自动构建地址：{前缀}/{ValueType名称}
                    var prefix = _settings?.ConfigAddressPrefix;
                    address = string.IsNullOrEmpty(prefix)
                        ? valueType.Name
                        : $"{prefix}/{valueType.Name}";
                }
            }

            // 通过反射获取 TKey
            var keyType = GetKeyType(valueType);
            if (keyType == null)
            {
                Debug.LogError($"[ConfigService] 无法解析 {valueType.Name} 的主键类型，确保实现了 IConfigItem<TKey>");
                return;
            }

            // 反射调用 provider.LoadAsync<TKey, TValue>(address)
            var table = await InvokeProviderLoad(keyType, valueType, address, ct);
            if (table != null)
            {
                _tables[valueType] = table;
                _addresses[valueType] = address;
            }
        }

        public async UniTask LoadAllAsync(CancellationToken ct = default)
        {
            foreach (var kvp in _addresses.ToList())
            {
                ct.ThrowIfCancellationRequested();

                if (_tables.ContainsKey(kvp.Key)) continue;

                var keyType = GetKeyType(kvp.Key);
                if (keyType == null) continue;

                await InvokeProviderLoad(keyType, kvp.Key, kvp.Value, ct);
            }
        }

        public ConfigTable<TKey, TValue> GetTable<TKey, TValue>()
            where TValue : IConfigItem<TKey>
        {
            if (_tables.TryGetValue(typeof(TValue), out var table))
                return table as ConfigTable<TKey, TValue>;
            return null;
        }

        public bool TryGetTable<TKey, TValue>(out ConfigTable<TKey, TValue> table)
            where TValue : IConfigItem<TKey>
        {
            table = GetTable<TKey, TValue>();
            return table != null;
        }

        public TValue Get<TKey, TValue>(TKey key) where TValue : IConfigItem<TKey>
        {
            var table = GetTable<TKey, TValue>();
            return table != null ? table.Get(key) : default;
        }

        public async UniTask ReloadAsync<TValue>(string address = null, CancellationToken ct = default)
            where TValue : IConfigItem
        {
            Unload<TValue>();
            await LoadAsync<TValue>(address, ct);
        }

        public void Unload<TValue>() where TValue : IConfigItem
        {
            var valueType = typeof(TValue);

            if (_tables.TryGetValue(valueType, out var table))
            {
                if (_addresses.TryGetValue(valueType, out var address))
                    _provider.Release(address);

                _tables.Remove(valueType);
            }
        }

        public void UnloadAll()
        {
            foreach (var kvp in _addresses)
                _provider.Release(kvp.Value);

            _tables.Clear();
        }

        public void RegisterAddress<TValue>(string address) where TValue : IConfigItem
        {
            _addresses[typeof(TValue)] = address;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnloadAll();
            _provider?.Dispose();
        }

        #region 反射辅助

        /// <summary>
        ///     获取 IConfigItem&lt;TKey&gt; 中的 TKey 类型
        /// </summary>
        private static Type GetKeyType(Type valueType)
        {
            foreach (var iface in valueType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IConfigItem<>))
                    return iface.GetGenericArguments()[0];
            }

            return null;
        }

        /// <summary>
        ///     通过反射调用 IConfigProvider.LoadAsync&lt;TKey, TValue&gt;(address, ct)
        /// </summary>
        private async UniTask<object> InvokeProviderLoad(Type keyType, Type valueType, string address,
            CancellationToken ct)
        {
            try
            {
                var loadMethod = typeof(ConfigService).GetMethod(nameof(InvokeLoadInternal),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var genericMethod = loadMethod.MakeGenericMethod(keyType, valueType);
                var task = (UniTask)genericMethod.Invoke(this, new object[] { address, ct });
                await task;

                return _tables.TryGetValue(valueType, out var table) ? table : null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigService] 加载配置失败: {address}, 错误: {ex.InnerException?.Message ?? ex.Message}");
                return null;
            }
        }

        /// <summary>
        ///     泛型加载辅助（供反射调用）
        /// </summary>
        [Preserve]
        private async UniTask InvokeLoadInternal<TKey, TValue>(string address, CancellationToken ct)
            where TValue : IConfigItem<TKey>
        {
            var table = await _provider.LoadAsync<TKey, TValue>(address, ct);
            if (table != null)
            {
                _tables[typeof(TValue)] = table;
                _addresses[typeof(TValue)] = address;
            }
        }

        #endregion
    }
}
