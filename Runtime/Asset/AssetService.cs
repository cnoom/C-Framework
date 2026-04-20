using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     资源服务实现（基于 IAssetProvider）
    /// </summary>
    public sealed class AssetService : IAssetService, IDisposable
    {
        private readonly Dictionary<object, bool> _instanceFlags = new();
        private readonly Dictionary<object, Object> _loadedAssets = new();
        private readonly Dictionary<object, UniTaskCompletionSource<Object>> _loadingTasks = new();
        private readonly object _lock = new();
        private readonly IAssetProvider _provider;
        private readonly Dictionary<object, int> _refCounts = new();

        public AssetService(FrameworkSettings settings, IAssetProvider provider = null)
        {
            _provider = provider ?? new AddressableAssetProvider();
            MemoryBudget = new AssetMemoryBudget
            {
                BudgetBytes = settings.MemoryBudgetMB * 1024L * 1024L
            };
        }

        public AssetMemoryBudget MemoryBudget { get; }

        public async UniTask<AssetHandle> LoadAsync<T>(object key, CancellationToken ct = default) where T : Object
        {
            UniTaskCompletionSource<Object> loadingTcs = null;
            bool isLoader;

            lock (_lock)
            {
                // 已加载完成：增加引用计数并返回
                if (_refCounts.TryGetValue(key, out _) && !_loadingTasks.ContainsKey(key))
                {
                    _refCounts[key]++;
                    return new AssetHandle(_loadedAssets[key], this, key);
                }

                // 正在加载中：等待已存在的加载任务完成
                if (_loadingTasks.TryGetValue(key, out var existingTcs))
                {
                    loadingTcs = existingTcs;
                    isLoader = false;
                }
                else
                {
                    // 首次加载：创建占位任务，当前请求即为发起者
                    loadingTcs = new UniTaskCompletionSource<Object>();
                    _loadingTasks[key] = loadingTcs;
                    _refCounts[key] = 1;
                    isLoader = true;
                }
            }

            if (!isLoader)
            {
                // 等待发起者完成加载
                var loadedAsset = await loadingTcs.Task;
                lock (_lock)
                {
                    // 检查资源是否已被释放（竞态条件：await 期间其他调用者可能已 Release）
                    if (!_loadedAssets.ContainsKey(key))
                    {
                        // 资源已释放，跳出等待路径，递归重新加载
                    }
                    else
                    {
                        _refCounts.TryGetValue(key, out var count);
                        _refCounts[key] = count + 1;
                        return new AssetHandle(loadedAsset as T, this, key);
                    }
                }

                // 资源已被释放，静默重新加载
                return await LoadAsync<T>(key, ct);
            }

            // 当前请求是加载发起者，执行实际加载
            var asset = await _provider.LoadAssetAsync<T>(key, ct);

            lock (_lock)
            {
                _loadedAssets[key] = asset;
                _loadingTasks.Remove(key);

                // 更新内存使用
                MemoryBudget.UsedBytes += _provider.GetAssetMemorySize(key);
                MemoryBudget.CheckBudget();
            }

            // 通知所有等待者
            loadingTcs.TrySetResult(asset);

            return new AssetHandle(asset, this, key);
        }

        public async UniTask<GameObject> InstantiateAsync(object key, Transform parent = null,
            CancellationToken ct = default)
        {
            var instance = await _provider.InstantiateAsync(key, parent, ct);

            // 使用独立前缀避免与 LoadAsync 的 key 冲突
            var instKey = "$inst_" + key;
            lock (_lock)
            {
                _instanceFlags[instKey] = true;
                _refCounts.TryGetValue(instKey, out var count);
                _refCounts[instKey] = count + 1;
            }

            return instance;
        }

        public IDisposable LinkToScope(object key, object scope)
        {
            // 如果 scope 是 GameObject，绑定到其生命周期
            if (scope is GameObject go)
            {
                // 增加引用计数
                lock (_lock)
                {
                    if (!_refCounts.TryGetValue(key, out var count))
                        throw new InvalidOperationException($"Asset not loaded: {key}");
                    _refCounts[key] = count + 1;
                }

                var tracker = go.AddComponent<AssetLifetimeTracker>();
                tracker.Initialize(key, this);
                return new GameObjectBinding(tracker);
            }

            // 对于普通 IDisposable，返回组合 Disposable
            if (scope is IDisposable disposable) return new ScopeLink(key, this, disposable);

            // 其他类型不支持
            throw new ArgumentException($"Unsupported scope type: {scope?.GetType().Name}", nameof(scope));
        }

        public void Release(object key)
        {
            lock (_lock)
            {
                if (!_refCounts.TryGetValue(key, out var count)) return;

                count--;
                if (count <= 0)
                {
                    // 引用计数归零，释放资源
                    var isInstance = _instanceFlags.ContainsKey(key);

                    if (_loadedAssets.ContainsKey(key) || isInstance)
                    {
                        MemoryBudget.UsedBytes -= _provider.GetAssetMemorySize(key);
                        _provider.ReleaseHandle(key, isInstance);
                        _loadedAssets.Remove(key);
                        _instanceFlags.Remove(key);
                    }

                    _refCounts.Remove(key);
                }
                else
                {
                    _refCounts[key] = count;
                }
            }
        }

        public void ReleaseAll()
        {
            lock (_lock)
            {
                foreach (var kvp in _loadedAssets)
                    _provider.ReleaseHandle(kvp.Key, false);

                foreach (var kvp in _instanceFlags)
                    _provider.ReleaseHandle(kvp.Key, true);

                _loadedAssets.Clear();
                _instanceFlags.Clear();
                _refCounts.Clear();
                MemoryBudget.UsedBytes = 0;
            }
        }

        public async UniTask PreloadAsync(
            IEnumerable<object> keys,
            IProgress<float> progress = null,
            int maxLoadPerFrame = 5,
            CancellationToken ct = default)
        {
            var keyList = new List<object>(keys);
            var processed = 0;
            var frameLoaded = 0;

            foreach (var key in keyList)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await LoadAsync<Object>(key, ct);
                    frameLoaded++;

                    // 分帧加载
                    if (frameLoaded >= maxLoadPerFrame)
                    {
                        frameLoaded = 0;
                        await UniTask.Yield(ct);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AssetService] Failed to preload: {key}, Error: {ex.Message}");
                }

                // 每处理一个资源就更新进度
                processed++;
                progress?.Report((float)processed / keyList.Count);
            }
        }

        public void Dispose()
        {
            ReleaseAll();
        }

        /// <summary>
        ///     生命周期绑定器（用于普通 IDisposable）
        /// </summary>
        private sealed class ScopeLink : IDisposable
        {
            private bool _disposed;
            private object _key;
            private IDisposable _scope;
            private AssetService _service;

            public ScopeLink(object key, AssetService service, IDisposable scope)
            {
                _key = key;
                _service = service;
                _scope = scope;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                _service?.Release(_key);
                _scope?.Dispose();

                _key = null;
                _service = null;
                _scope = null;
            }
        }

        /// <summary>
        ///     GameObject 绑定包装器
        /// </summary>
        private sealed class GameObjectBinding : IDisposable
        {
            private bool _disposed;
            private AssetLifetimeTracker _tracker;

            public GameObjectBinding(AssetLifetimeTracker tracker)
            {
                _tracker = tracker;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                // 销毁 tracker 组件（会触发 OnDestroy 释放资源）
                if (_tracker != null && _tracker.gameObject != null) Object.Destroy(_tracker);

                _tracker = null;
            }
        }

        /// <summary>
        ///     资源生命周期追踪器（用于 GameObject）
        /// </summary>
        private sealed class AssetLifetimeTracker : MonoBehaviour
        {
            private object _key;
            private bool _released;
            private AssetService _service;

            private void OnDestroy()
            {
                if (_released) return;
                _released = true;

                _service?.Release(_key);
                _key = null;
                _service = null;
            }

            public void Initialize(object key, AssetService service)
            {
                _key = key;
                _service = service;
            }
        }
    }
}
