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
        ///     释放资源句柄
        /// </summary>
        void ReleaseHandle(object key, bool isInstance);

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
        ///     实例化预制体
        /// </summary>
        UniTask<GameObject> InstantiateAsync(object key, Transform parent = null, CancellationToken ct = default);

        /// <summary>
        ///     将资源绑定到指定生命周期
        /// </summary>
        /// <param name="key">资源 key</param>
        /// <param name="scope">生命周期对象（支持 GameObject 或 IDisposable）</param>
        /// <returns>可释放的绑定对象</returns>
        IDisposable LinkToScope(object key, object scope);

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