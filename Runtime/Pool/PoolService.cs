using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     对象池管理服务实现
    ///     <para>统一管理所有 ObjectPool 和 GameObjectPool</para>
    ///     <para>通过 DI 注入，在 FrameworkModuleInstaller 中注册</para>
    /// </summary>
    public sealed class PoolService : IPoolService, IDisposable
    {
        private readonly Dictionary<string, object> _pools = new();
        private readonly IAssetService _assetService;
        private readonly FrameworkSettings _settings;
        private readonly object _lock = new();
        private bool _disposed;

        public PoolService(FrameworkSettings settings, IAssetService assetService)
        {
            _settings = settings;
            _assetService = assetService;
        }

        /// <inheritdoc />
        public IPool<T> GetPool<T>(
            string name,
            Func<T> createFunc,
            Action<T> actionOnGet = null,
            Action<T> actionOnReturn = null,
            Action<T> actionOnDestroy = null,
            int defaultCapacity = 0,
            int maxSize = 0) where T : class
        {
            var key = $"{name}_{typeof(T).FullName}";

            lock (_lock)
            {
                if (_pools.TryGetValue(key, out var existing))
                {
                    return (IPool<T>)existing;
                }

                var capacity = defaultCapacity > 0 ? defaultCapacity : _settings.PoolDefaultCapacity;
                var max = maxSize > 0 ? maxSize : _settings.PoolMaxSize;

                var pool = new ObjectPool<T>(
                    name,
                    createFunc,
                    actionOnGet,
                    actionOnReturn,
                    actionOnDestroy,
                    capacity,
                    max
                );

                _pools[key] = pool;
                return pool;
            }
        }

        /// <inheritdoc />
        public IPool<GameObject> GetGameObjectPool(
            string name,
            object prefabKey,
            Transform parent = null,
            int defaultCapacity = 0,
            int maxSize = 0)
        {
            lock (_lock)
            {
                if (_pools.TryGetValue(name, out var existing))
                {
                    return (IPool<GameObject>)existing;
                }

                // 创建池根节点
                if (parent == null)
                {
                    var root = new GameObject($"[Pool_{name}]");
                    Object.DontDestroyOnLoad(root);
                    parent = root.transform;
                }

                var capacity = defaultCapacity > 0 ? defaultCapacity : _settings.PoolDefaultCapacity;
                var max = maxSize > 0 ? maxSize : _settings.PoolMaxSize;

                var pool = new GameObjectPool(
                    name,
                    _assetService,
                    prefabKey,
                    parent,
                    capacity,
                    max
                );

                _pools[name] = pool;
                return pool;
            }
        }

        /// <inheritdoc />
        public void DestroyPool(string name)
        {
            lock (_lock)
            {
                if (_pools.Remove(name, out var pool))
                {
                    (pool as IDisposable)?.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public void DestroyAllPools()
        {
            lock (_lock)
            {
                foreach (var pool in _pools.Values)
                {
                    (pool as IDisposable)?.Dispose();
                }

                _pools.Clear();
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<IPoolInfo> GetAllPoolInfo()
        {
            lock (_lock)
            {
                var result = new List<IPoolInfo>(_pools.Count);

                foreach (var pool in _pools.Values)
                {
                    if (pool is IPoolInfo info)
                    {
                        result.Add(info);
                    }
                }

                return result;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                DestroyAllPools();
            }
        }
    }
}
