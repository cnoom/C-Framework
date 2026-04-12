using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     配置服务实现（基于Addressables）
    /// </summary>
    public sealed class ConfigService : IConfigService, IDisposable
    {
        private readonly IAssetService _assetService;

        /// <summary>
        ///     缓存反射调用委托，避免每次 Get 都走 MakeGenericType
        /// </summary>
        private readonly ConcurrentDictionary<Type, Func<ConfigTableBase, object, object>> _getDelegates = new();

        private readonly Dictionary<Type, AssetHandle> _handles = new();
        private readonly FrameworkSettings _settings;
        private readonly Dictionary<Type, ConfigTableBase> _tables = new();

        public ConfigService(FrameworkSettings settings, IAssetService assetService)
        {
            _settings = settings;
            _assetService = assetService;
        }

        public async UniTask LoadAsync<TKey>(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var type = typeof(TKey);

            // 如果已加载，直接返回
            if (_tables.ContainsKey(type)) return;

            // 构建地址：{前缀}/{表名}
            var address = string.IsNullOrEmpty(_settings.ConfigAddressPrefix)
                ? type.Name
                : $"{_settings.ConfigAddressPrefix}/{type.Name}";

            try
            {
                // 使用 Addressables 加载
                var handle = await _assetService.LoadAsync<ConfigTableBase>(address, ct);

                if (handle.Asset is ConfigTableBase table)
                {
                    _tables[type] = table;
                    _handles[type] = handle;
                    table.IsLoaded = true;
                }
                else
                {
                    handle.Dispose();
                    Debug.LogWarning($"[ConfigService] 配置表加载失败，资源类型不匹配: {address}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigService] 配置表加载失败: {address}, 错误: {ex.Message}");
                throw;
            }
        }

        public UniTask LoadAllAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException(
                "[ConfigService] LoadAllAsync 在 Addressables 模式下未实现，请使用 LoadAsync<TKey> 逐个加载或通过标签预加载");
        }

        public T GetTable<T>() where T : ConfigTableBase
        {
            if (_tables.TryGetValue(typeof(T), out var table)) return table as T;
            return null;
        }

        public bool TryGetTable<T>(out T table) where T : ConfigTableBase
        {
            if (_tables.TryGetValue(typeof(T), out var t))
            {
                table = t as T;
                return table != null;
            }

            table = null;
            return false;
        }

        public TValue Get<TKey, TValue>(TKey key)
        {
            var tableType = typeof(ConfigTable<,>).MakeGenericType(typeof(TKey), typeof(TValue));

            if (_tables.TryGetValue(tableType, out var table))
            {
                var del = _getDelegates.GetOrAdd(tableType, t =>
                {
                    var getMethod = t.GetMethod("Get");
                    return (tbl, k) => getMethod.Invoke(tbl, new[] { k });
                });
                return (TValue)del(table, key);
            }

            return default;
        }

        public async UniTask ReloadAsync<TKey>(CancellationToken ct = default)
        {
            var type = typeof(TKey);

            // 移除已加载的配置
            if (_tables.ContainsKey(type)) _tables.Remove(type);

            // 释放资源句柄
            if (_handles.TryGetValue(type, out var handle))
            {
                handle.Dispose();
                _handles.Remove(type);
            }

            // 重新加载
            await LoadAsync<TKey>(ct);
        }

        public void Dispose()
        {
            // 释放所有资源句柄
            foreach (var handle in _handles.Values) handle.Dispose();

            _handles.Clear();
            _tables.Clear();
        }
    }
}