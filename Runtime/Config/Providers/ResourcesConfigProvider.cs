using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     基于 Resources 目录的配置数据加载提供者
    ///     <para>从 Resources 文件夹加载 ConfigTableAsset SO，无需 Addressables</para>
    ///     <para>适用于小型项目或原型阶段</para>
    /// </summary>
    public class ResourcesConfigProvider : IConfigProvider
    {
        private readonly Dictionary<string, UnityEngine.Object> _loadedAssets = new();
        private readonly HashSet<string> _loadedAddresses = new();
        private bool _disposed;

        public UniTask<ConfigTable<TKey, TValue>> LoadAsync<TKey, TValue>(string address,
            CancellationToken ct = default)
            where TValue : IConfigItem<TKey>
        {
            var asset = Resources.Load<ConfigTableAsset>(address);

            if (asset == null)
            {
                Debug.LogError($"[ResourcesConfigProvider] 加载失败，Resources 中未找到: {address}");
                return UniTask.FromResult<ConfigTable<TKey, TValue>>(null);
            }

            if (asset is ConfigTableAsset<TKey, TValue> typedAsset)
            {
                var table = new ConfigTable<TKey, TValue>();
                typedAsset.PopulateTable(table);
                _loadedAssets[address] = asset;
                _loadedAddresses.Add(address);
                return UniTask.FromResult(table);
            }

            Debug.LogError(
                $"[ResourcesConfigProvider] 类型不匹配: {address}，" +
                $"期望 ConfigTableAsset<{typeof(TKey).Name}, {typeof(TValue).Name}>，" +
                $"实际 {asset.GetType().Name}");
            return UniTask.FromResult<ConfigTable<TKey, TValue>>(null);
        }

        public void Release(string address)
        {
            if (_loadedAddresses.Remove(address))
            {
                if (_loadedAssets.TryGetValue(address, out var asset))
                {
                    Resources.UnloadAsset(asset);
                    _loadedAssets.Remove(address);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var kvp in _loadedAssets)
                Resources.UnloadAsset(kvp.Value);

            _loadedAssets.Clear();
            _loadedAddresses.Clear();
        }
    }
}
