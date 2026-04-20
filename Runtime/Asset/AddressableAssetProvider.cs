using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     基于 Addressables 的资源加载提供者（默认实现）
    /// </summary>
    public sealed class AddressableAssetProvider : IAssetProvider
    {
        private readonly Dictionary<object, AsyncOperationHandle> _handles = new();

        public async UniTask<Object> LoadAssetAsync<T>(object key, CancellationToken ct = default) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
                throw new System.Exception($"Failed to load asset: {key}");
            }

            lock (_handles)
            {
                _handles[key] = handle;
            }

            return handle.Result;
        }

        public async UniTask<GameObject> InstantiateAsync(object key, Transform parent,
            CancellationToken ct = default)
        {
            var handle = Addressables.InstantiateAsync(key, parent);
            await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
                throw new System.Exception($"Failed to instantiate: {key}");
            }

            var instKey = "$inst_" + key;
            lock (_handles)
            {
                _handles[instKey] = handle;
            }

            return handle.Result;
        }

        public void ReleaseHandle(object key, bool isInstance)
        {
            lock (_handles)
            {
                if (_handles.TryGetValue(key, out var handle))
                {
                    Addressables.Release(handle);
                    _handles.Remove(key);
                }
            }
        }

        public long GetAssetMemorySize(object key)
        {
            // TODO: Addressables 没有按资源查询内存的公开 API
            // 当前返回估算值，后续可通过 Profiler 或自定义追踪实现精确计算
            lock (_handles)
            {
                if (!_handles.TryGetValue(key, out var handle)) return 0L;
                if (handle.Result is Texture tex)
                    return tex.width * tex.height * (tex.graphicsFormat != 0 ? 4 : 4);
                if (handle.Result is AudioClip clip)
                    // 估算：采样数 × 声道数 × 2字节（假设16位PCM）
                    return clip.samples * clip.channels * 2L;
                return 1024L;
            }
        }
    }
}
