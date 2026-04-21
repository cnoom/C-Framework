using System;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     实例化句柄，持有即拥有，Dispose 即释放
    ///     <para>与 AssetHandle（共享引用计数）不同，InstanceHandle 表示独占所有权</para>
    /// </summary>
    public sealed class InstanceHandle : IDisposable
    {
        private IAssetProvider _provider;
        private bool _disposed;

        /// <summary>
        ///     实例化的 GameObject
        /// </summary>
        public GameObject GameObject { get; }

        /// <summary>
        ///     是否已被释放
        /// </summary>
        public bool IsDisposed => _disposed;

        internal InstanceHandle(GameObject instance, IAssetProvider provider)
        {
            GameObject = instance;
            _provider = provider;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_provider != null)
            {
                _provider.ReleaseInstance(GameObject);
                _provider = null;
            }
        }
    }
}
