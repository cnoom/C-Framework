using System;
using System.Collections.Generic;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     对象池管理服务接口
    ///     <para>统一管理所有对象池的生命周期</para>
    ///     <para>通过 DI 注入使用</para>
    /// </summary>
    public interface IPoolService
    {
        /// <summary>
        ///     获取或创建 C# 对象池
        /// </summary>
        /// <param name="name">池名称</param>
        /// <param name="createFunc">对象工厂方法</param>
        /// <param name="actionOnGet">获取时回调（可选）</param>
        /// <param name="actionOnReturn">归还时回调（可选）</param>
        /// <param name="actionOnDestroy">销毁时回调（可选）</param>
        /// <param name="defaultCapacity">默认容量</param>
        /// <param name="maxSize">最大容量（0=不限）</param>
        IPool<T> GetPool<T>(
            string name,
            Func<T> createFunc,
            Action<T> actionOnGet = null,
            Action<T> actionOnReturn = null,
            Action<T> actionOnDestroy = null,
            int defaultCapacity = 0,
            int maxSize = 0) where T : class;

        /// <summary>
        ///     获取或创建 GameObject 对象池
        /// </summary>
        /// <param name="name">池名称</param>
        /// <param name="prefabKey">Prefab 的 Addressable Key</param>
        /// <param name="parent">默认父节点（可选）</param>
        /// <param name="defaultCapacity">默认容量</param>
        /// <param name="maxSize">最大容量（0=不限）</param>
        IPool<GameObject> GetGameObjectPool(
            string name,
            object prefabKey,
            Transform parent = null,
            int defaultCapacity = 0,
            int maxSize = 0);

        /// <summary>
        ///     销毁指定名称的池
        /// </summary>
        void DestroyPool(string name);

        /// <summary>
        ///     销毁所有池
        /// </summary>
        void DestroyAllPools();

        /// <summary>
        ///     获取所有池的调试信息
        /// </summary>
        IReadOnlyList<IPoolInfo> GetAllPoolInfo();
    }
}
