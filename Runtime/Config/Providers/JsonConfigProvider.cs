using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     基于 JSON 文件的配置数据加载提供者
    ///     <para>通过 Addressables 加载 TextAsset（JSON），反序列化后注入 ConfigTable</para>
    ///     <para>JSON 格式要求：数组形式，如 [{"Id":1,"Name":"xx"}, ...]</para>
    /// </summary>
    public class JsonConfigProvider : IConfigProvider
    {
        private readonly IAssetService _assetService;
        private readonly HashSet<string> _loadedAddresses = new();
        private bool _disposed;

        public JsonConfigProvider(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public async UniTask<ConfigTable<TKey, TValue>> LoadAsync<TKey, TValue>(string address)
            where TValue : class, IConfigItem<TKey>
        {
            var handle = await _assetService.LoadAsync<TextAsset>(address);
            var textAsset = handle.Asset as TextAsset;

            if (textAsset == null)
            {
                Debug.LogError($"[JsonConfigProvider] 加载失败，资源不是 TextAsset: {address}");
                return null;
            }

            // JsonUtility 不支持直接反序列化顶层数组，需要包装
            var json = textAsset.text;
            var wrapperJson = $"{{\"Items\":{json}}}";
            var wrapper = JsonUtility.FromJson<ConfigDataWrapper<TValue>>(wrapperJson);

            if (wrapper?.Items == null)
            {
                Debug.LogError($"[JsonConfigProvider] JSON 反序列化失败: {address}");
                return null;
            }

            var table = new ConfigTable<TKey, TValue>();
            table.Load(wrapper.Items);
            _loadedAddresses.Add(address);

            return table;
        }

        public void Release(string address)
        {
            if (_loadedAddresses.Remove(address))
                _assetService?.Release(address);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var address in _loadedAddresses)
                _assetService?.Release(address);

            _loadedAddresses.Clear();
        }

        /// <summary>
        ///     JSON 反序列化包装类
        ///     <para>JsonUtility 不支持顶层数组，需要用对象包裹</para>
        /// </summary>
        [Serializable]
        private class ConfigDataWrapper<T>
        {
            public List<T> Items;
        }
    }
}
