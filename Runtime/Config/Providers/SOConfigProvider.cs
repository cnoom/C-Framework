using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     基于 ScriptableObject / Addressables 的配置数据加载提供者
    ///     <para>加载 ConfigTableAsset SO 资产，将数据注入纯 C# 的 ConfigTable</para>
    /// </summary>
    public class SOConfigProvider : IConfigProvider
    {
        private readonly IAssetService _assetService;
        private bool _disposed;

        public SOConfigProvider(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public async UniTask<ConfigTable<TKey, TValue>> LoadAsync<TKey, TValue>(string address,
            CancellationToken ct = default)
            where TValue : IConfigItem<TKey>
        {
            var handle = await _assetService.LoadAsync<ConfigTableAsset>(address, ct);

            if (handle.Asset is ConfigTableAsset<TKey, TValue> typedAsset)
            {
                var table = new ConfigTable<TKey, TValue>();
                typedAsset.PopulateTable(table);
                return table;
            }

            LogUtility.Error("SOConfigProvider", $"加载失败，类型不匹配: {address}，" +
                           $"期望 ConfigTableAsset<{typeof(TKey).Name}, {typeof(TValue).Name}>，" +
                           $"实际 {handle.Asset?.GetType().Name ?? "null"}");
            return null;
        }

        public void Release(string address)
        {
            _assetService?.Release(address);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
