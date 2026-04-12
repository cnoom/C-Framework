using System;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     资源句柄，封装引用计数逻辑
    /// </summary>
    public readonly struct AssetHandle : IDisposable
    {
        public Object Asset { get; }

        private readonly IAssetService _service;
        private readonly object _key;

        internal AssetHandle(Object asset, IAssetService service, object key)
        {
            Asset = asset;
            _service = service;
            _key = key;
        }

        public void Dispose()
        {
            if (_service != null && _key != null) _service.Release(_key);
        }

        /// <summary>
        ///     获取资源并转换类型
        /// </summary>
        public T As<T>() where T : Object
        {
            return Asset as T;
        }
    }
}