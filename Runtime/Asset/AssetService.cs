using System;
using System.Collections.Generic;
using System.Threading;
using CFramework.Profiling;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     资源服务实现（基于Addressables）
    /// </summary>
    public sealed class AssetService : IAssetService, IDisposable
    {
        private readonly Dictionary<object, AsyncOperationHandle> _handles = new();
        private readonly object _lock = new();
        private readonly Dictionary<object, int> _refCounts = new();
        private readonly FrameworkSettings _settings;

        public AssetService(FrameworkSettings settings)
        {
            _settings = settings;
            MemoryBudget = new AssetMemoryBudget
            {
                BudgetBytes = settings.MemoryBudgetMB * 1024L * 1024L
            };
        }

        public AssetMemoryBudget MemoryBudget { get; }

        public async UniTask<AssetHandle> LoadAsync<T>(object key, CancellationToken ct = default) where T : Object
        {
            lock (_lock)
            {
                if (_refCounts.TryGetValue(key, out _))
                {
                    _refCounts[key]++;
                    var existingHandle = _handles[key];
                    return new AssetHandle(existingHandle.Result as T, this, key);
                }
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status != AsyncOperationStatus.Succeeded) throw new Exception($"Failed to load asset: {key}");

            lock (_lock)
            {
                _handles[key] = handle;
                _refCounts[key] = 1;

                // 更新内存使用
                MemoryBudget.UsedBytes += Profiler.GetAllocatedMemoryForHandle(handle);
                MemoryBudget.CheckBudget();
            }

            return new AssetHandle(handle.Result, this, key);
        }

        public async UniTask<GameObject> InstantiateAsync(object key, Transform parent = null,
            CancellationToken ct = default)
        {
            var handle = Addressables.InstantiateAsync(key, parent);
            await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status != AsyncOperationStatus.Succeeded) throw new Exception($"Failed to instantiate: {key}");

            // 使用独立前缀避免与 LoadAsync 的 key 冲突
            var instKey = "$inst_" + key;
            lock (_lock)
            {
                _handles[instKey] = handle;
                _refCounts.TryGetValue(instKey, out var count);
                _refCounts[instKey] = count + 1;
            }

            return handle.Result;
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
                    if (_handles.TryGetValue(key, out var handle))
                    {
                        // 更新内存使用
                        MemoryBudget.UsedBytes -= Profiler.GetAllocatedMemoryForHandle(handle);
                        Addressables.Release(handle);
                        _handles.Remove(key);
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
                foreach (var handle in _handles.Values) Addressables.Release(handle);
                _handles.Clear();
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

// 内部使用的Profiler辅助类
namespace CFramework.Profiling
{
    internal static class Profiler
    {
        public static long GetAllocatedMemoryForHandle(AsyncOperationHandle handle)
        {
            // 简化实现，实际项目中可以使用更精确的内存统计
            return handle.IsValid() && handle.Result is Object ? 1024L : 0L;
        }
    }
}