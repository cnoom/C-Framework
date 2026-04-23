using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
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
        private readonly Dictionary<GameObject, AsyncOperationHandle> _instanceHandles = new();

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

            var instance = handle.Result;
            lock (_instanceHandles)
            {
                _instanceHandles[instance] = handle;
            }

            return instance;
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return;

            lock (_instanceHandles)
            {
                if (_instanceHandles.TryGetValue(instance, out var handle))
                {
                    _instanceHandles.Remove(instance);
                    Addressables.Release(handle);
                }
            }
        }

        public void ReleaseHandle(object key)
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
                    return EstimateTextureMemorySize(tex);
                if (handle.Result is AudioClip clip)
                    // 估算：采样数 × 声道数 × 2字节（假设16位PCM）
                    return clip.samples * clip.channels * 2L;
                return 1024L;
            }
        }

        /// <summary>
        ///     估算纹理内存占用（字节）
        ///     <para>压缩格式按块大小计算，非压缩格式按每像素字节数计算</para>
        /// </summary>
        private static long EstimateTextureMemorySize(Texture tex)
        {
            var format = tex.graphicsFormat;

            if (GraphicsFormatUtility.IsCompressedFormat(format))
            {
                // 压缩格式：按块计算（通常 4×4 像素/块）
                var blockSize = GraphicsFormatUtility.GetBlockSize(format);
                return (long)((tex.width + 3) / 4) * ((tex.height + 3) / 4) * blockSize;
            }

            // 非压缩格式：每像素字节数 = GetBlockSize
            var bytesPerPixel = GraphicsFormatUtility.GetBlockSize(format);
            return (long)tex.width * tex.height * bytesPerPixel;
        }
    }
}
