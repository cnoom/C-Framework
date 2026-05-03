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
        private readonly Dictionary<GameObject, AsyncOperationHandle> _instanceHandles = new();

        public async UniTask<Object> LoadAssetAsync<T>(object key, CancellationToken ct = default) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
                throw new System.Exception($"资源加载失败: {key}");
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
                throw new System.Exception($"实例化失败: {key}");
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
        ///     <para>Texture2D 按 TextureFormat 精确计算，其他类型按 RGBA32 估算</para>
        /// </summary>
        private static long EstimateTextureMemorySize(Texture tex)
        {
            if (tex is Texture2D tex2d)
                return (long)tex.width * tex.height * GetBytesPerPixel(tex2d.format);

            // RenderTexture 等其他类型，默认按 RGBA32 估算
            return (long)tex.width * tex.height * 4L;
        }

        /// <summary>
        ///     根据 TextureFormat 获取每像素字节数
        ///     <para>压缩格式按等效字节/像素返回（基于 4x4 块大小折算）</para>
        /// </summary>
        private static int GetBytesPerPixel(TextureFormat format)
        {
            return format switch
            {
                // 非压缩格式
                TextureFormat.R8 => 1,
                TextureFormat.R16 => 2,
                TextureFormat.RG16 => 2,
                TextureFormat.RGB24 => 3,
                TextureFormat.RGBA32 => 4,
                TextureFormat.ARGB32 => 4,
                TextureFormat.RHalf => 2,
                TextureFormat.RGHalf => 4,
                TextureFormat.RGBAHalf => 8,
                TextureFormat.RFloat => 4,
                TextureFormat.RGFloat => 8,
                TextureFormat.RGBAFloat => 16,
                // 压缩格式：≈ 0.5 byte/px，向上取整为 1
                TextureFormat.DXT1 or TextureFormat.DXT1Crunched or TextureFormat.BC4
                    or TextureFormat.EAC_R or TextureFormat.EAC_R_SIGNED
                    or TextureFormat.ETC_RGB4 or TextureFormat.ETC_RGB4_3DS
                    or TextureFormat.ETC_RGB4Crunched or TextureFormat.PVRTC_RGB2
                    or TextureFormat.PVRTC_RGBA2 => 1,
                // 压缩格式：≈ 1 byte/px
                TextureFormat.DXT5 or TextureFormat.DXT5Crunched or TextureFormat.BC5
                    or TextureFormat.BC6H or TextureFormat.BC7
                    or TextureFormat.EAC_RG or TextureFormat.EAC_RG_SIGNED
                    or TextureFormat.ETC2_RGB or TextureFormat.ETC2_RGBA1
                    or TextureFormat.ETC2_RGBA8 or TextureFormat.ETC2_RGBA8Crunched
                    or TextureFormat.PVRTC_RGB4 or TextureFormat.PVRTC_RGBA4
                    or TextureFormat.ASTC_4x4 => 1,
                // ASTC 高压缩率格式：< 1 byte/px，估算为 1
                TextureFormat.ASTC_5x5 or TextureFormat.ASTC_6x6 or TextureFormat.ASTC_8x8
                    or TextureFormat.ASTC_10x10 or TextureFormat.ASTC_12x12 => 1,
                // 未知格式默认 4 字节/像素
                _ => 4
            };
        }
    }
}
