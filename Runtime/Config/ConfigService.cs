using System;
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
        private readonly Dictionary<Type, AssetHandle> _handles = new();
        private readonly FrameworkSettings _settings;
        private readonly Dictionary<Type, ConfigTableBase> _tables = new();

        public ConfigService(FrameworkSettings settings, IAssetService assetService)
        {
            _settings = settings;
            _assetService = assetService;
        }

        public async UniTask LoadAsync<TConfigTable>(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var type = typeof(TConfigTable);

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

        /// <summary>
        ///     通过主键获取配置数据
        ///     遍历已加载的配置表，查找继承自 ConfigTable&lt;TKey, TValue&gt; 的条目
        /// </summary>
        public TValue Get<TKey, TValue>(TKey key) where TValue : class, IConfigItem<TKey>
        {
            foreach (var kvp in _tables)
            {
                if (kvp.Value is ConfigTable<TKey, TValue> table)
                    return table.Get(key);
            }

            return default;
        }

        public async UniTask ReloadAsync<TConfigTable>(CancellationToken ct = default)
        {
            var type = typeof(TConfigTable);

            // 移除已加载的配置
            _tables.Remove(type);

            // 释放资源句柄
            if (_handles.TryGetValue(type, out var handle))
            {
                handle.Dispose();
                _handles.Remove(type);
            }

            // 重新加载
            await LoadAsync<TConfigTable>(ct);
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
