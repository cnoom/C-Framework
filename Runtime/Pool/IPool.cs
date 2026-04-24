using System;

namespace CFramework
{
    /// <summary>
    ///     对象池非泛型信息接口
    ///     <para>用于 PoolService 在不关心泛型参数时获取池状态</para>
    /// </summary>
    public interface IPoolInfo
    {
        /// <summary>
        ///     池名称（调试用）
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     池化对象类型名称
        /// </summary>
        string ItemTypeName { get; }

        /// <summary>
        ///     当前空闲对象数量
        /// </summary>
        int CountInactive { get; }

        /// <summary>
        ///     当前活跃（已租借）对象数量
        /// </summary>
        int CountActive { get; }

        /// <summary>
        ///     池中对象总数（活跃 + 空闲）
        /// </summary>
        int CountAll { get; }
    }

    /// <summary>
    ///     通用对象池接口
    ///     <para>提供对象的租借、归还、预分配、收缩等操作</para>
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    public interface IPool<T> : IPoolInfo, IDisposable where T : class
    {
        /// <summary>
        ///     从池中获取一个对象
        ///     <para>池为空时通过工厂方法创建新对象</para>
        /// </summary>
        T Get();

        /// <summary>
        ///     从池中获取一个对象，并返回自动归还句柄
        ///     <para>句柄 Dispose 时自动归还对象</para>
        /// </summary>
        PoolHandle<T> Get(out T item);

        /// <summary>
        ///     归还对象到池中
        /// </summary>
        void Return(T item);

        /// <summary>
        ///     归还所有已租借的对象
        /// </summary>
        void ReturnAll();

        /// <summary>
        ///     预分配指定数量的对象
        /// </summary>
        void Prewarm(int count);

        /// <summary>
        ///     清空池中所有空闲对象（不回收活跃对象）
        /// </summary>
        void Clear();

        /// <summary>
        ///     收缩池到指定容量（移除多余的空闲对象）
        /// </summary>
        void ShrinkTo(int capacity);
    }
}