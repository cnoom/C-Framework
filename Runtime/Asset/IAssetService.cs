using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CFramework
{
    /// <summary>
    ///     资源加载提供者接口
    ///     <para>抽象底层资源加载实现（Addressables、Resources 等），便于测试替换</para>
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        ///     异步加载资源
        /// </summary>
        UniTask<Object> LoadAssetAsync<T>(object key, CancellationToken ct = default) where T : Object;

        /// <summary>
        ///     异步实例化预制体
        /// </summary>
        UniTask<GameObject> InstantiateAsync(object key, Transform parent, CancellationToken ct = default);

        /// <summary>
        ///     释放实例（通过 GameObject 引用）
        /// </summary>
        void ReleaseInstance(GameObject instance);

        /// <summary>
        ///     释放资源句柄（通过 key）
        /// </summary>
        void ReleaseHandle(object key);

        /// <summary>
        ///     获取资源占用内存大小（字节）
        /// </summary>
        long GetAssetMemorySize(object key);
    }

    /// <summary>
    ///     资源服务接口
    /// </summary>
    public interface IAssetService
    {
        /// <summary>
        ///     内存预算
        /// </summary>
        AssetMemoryBudget MemoryBudget { get; }

        /// <summary>
        ///     加载资源，返回句柄
        /// </summary>
        UniTask<AssetHandle> LoadAsync<T>(object key, CancellationToken ct = default) where T : Object;

        /// <summary>
        ///     实例化预制体，返回独占句柄
        /// </summary>
        UniTask<InstanceHandle> InstantiateAsync(object key, Transform parent = null, CancellationToken ct = default);

        /// <summary>
        ///     将资源绑定到 GameObject 生命周期（GameObject 销毁时自动 Release）
        /// </summary>
        /// <param name="key">资源 key</param>
        /// <param name="scope">绑定的 GameObject</param>
        /// <returns>可释放的绑定对象</returns>
        IDisposable LinkToScope(object key, GameObject scope);

        /// <summary>
        ///     将资源绑定到 IDisposable 生命周期（Dispose 时自动 Release）
        /// </summary>
        /// <param name="key">资源 key</param>
        /// <param name="scope">绑定的 IDisposable 对象</param>
        /// <returns>可释放的绑定对象</returns>
        IDisposable LinkToScope(object key, IDisposable scope);

        /// <summary>
        ///     释放资源
        /// </summary>
        void Release(object key);

        /// <summary>
        ///     释放所有资源
        /// </summary>
        void ReleaseAll();

        /// <summary>
        ///     预加载资源
        /// </summary>
        UniTask PreloadAsync(
            IEnumerable<object> keys,
            IProgress<float> progress = null,
            int maxLoadPerFrame = 5,
            CancellationToken ct = default);
    }
}