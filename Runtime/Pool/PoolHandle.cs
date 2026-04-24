using System;

namespace CFramework
{
    /// <summary>
    ///     对象池自动归还句柄
    ///     <para>readonly struct 零 GC 分配</para>
    ///     <para>Dispose 时自动将对象归还到池中</para>
    ///     <para>支持 using 语法：using var handle = pool.Get(out var obj);</para>
    /// </summary>
    public readonly struct PoolHandle<T> : IDisposable where T : class
    {
        private readonly T _item;
        private readonly IPool<T> _pool;

        /// <summary>
        ///     获取的对象实例
        /// </summary>
        public T Item => _item;

        /// <summary>
        ///     所属池引用
        /// </summary>
        public IPool<T> Pool => _pool;

        internal PoolHandle(T item, IPool<T> pool)
        {
            _item = item;
            _pool = pool;
        }

        /// <summary>
        ///     归还对象到池中
        /// </summary>
        public void Dispose()
        {
            if (_pool != null && _item != null)
            {
                _pool.Return(_item);
            }
        }
    }
}
