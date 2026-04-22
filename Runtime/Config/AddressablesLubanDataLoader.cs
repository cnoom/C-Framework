using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     基于 Addressables 的 Luban 数据加载器
    ///     <para>通过 Addressables 加载配置表 bytes 文件，缓存后供 Luban Tables 同步读取</para>
    ///     <para>资源地址格式：{addressPrefix}/{tableFile}（如 "Config/TbItem"）</para>
    /// </summary>
    public class AddressablesLubanDataLoader : ILubanDataLoader
    {
        private readonly IAssetService _assetService;
        private readonly string _addressPrefix;
        private readonly Dictionary<string, byte[]> _cache = new();
        private readonly List<AssetHandle> _handles = new();
        private bool _disposed;

        /// <summary>
        ///     创建 Addressables Luban 数据加载器
        /// </summary>
        /// <param name="assetService">资源服务</param>
        /// <param name="addressPrefix">配置表 Addressable 地址前缀</param>
        public AddressablesLubanDataLoader(IAssetService assetService, string addressPrefix = "Config")
        {
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
            _addressPrefix = addressPrefix;
        }

        /// <summary>
        ///     异步预加载指定配置表的字节数据
        /// </summary>
        public async UniTask PreloadAsync(IEnumerable<string> tableFiles, CancellationToken ct = default)
        {
            if (tableFiles == null) throw new ArgumentNullException(nameof(tableFiles));

            foreach (var file in tableFiles)
            {
                ct.ThrowIfCancellationRequested();

                var address = $"{_addressPrefix}/{file}";
                var handle = await _assetService.LoadAsync<TextAsset>(address);

                if (handle.Asset is TextAsset textAsset)
                {
                    _cache[file] = textAsset.bytes;
                }
                else
                {
                    Debug.LogWarning($"[AddressablesLubanDataLoader] 配置表不是 TextAsset: {address}，" +
                                     $"实际类型: {handle.Asset?.GetType().Name ?? "null"}");
                }

                _handles.Add(handle);
            }
        }

        /// <summary>
        ///     同步获取已加载的字节数据
        /// </summary>
        public byte[] GetData(string tableFile)
        {
            return _cache.TryGetValue(tableFile, out var data) ? data : null;
        }

        /// <summary>
        ///     释放所有已加载的数据
        /// </summary>
        public void UnloadAll()
        {
            foreach (var handle in _handles)
                handle.Dispose();

            _handles.Clear();
            _cache.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnloadAll();
        }
    }
}
