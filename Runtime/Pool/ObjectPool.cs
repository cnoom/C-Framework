using System;
using System.Collections.Generic;

namespace CFramework
{
    /// <summary>
    ///     C# 对象池实现
    ///     <para>使用 Stack 存储空闲对象，LIFO 保证缓存友好</para>
    ///     <para>线程安全：lock 保护所有操作</para>
    /// </summary>
    public sealed class ObjectPool<T> : IPool<T> where T : class
    {
        private const int DefaultMaxSize = 1000;

        private readonly Stack<T> _pool;
        private readonly HashSet<T> _activeSet;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _actionOnGet;
        private readonly Action<T> _actionOnReturn;
        private readonly Action<T> _actionOnDestroy;
        private readonly int _maxSize;
        private readonly object _lock = new();
        private bool _disposed;

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string ItemTypeName => typeof(T).Name;

        /// <inheritdoc />
        public int CountInactive
        {
            get
            {
                lock (_lock) { return _pool.Count; }
            }
        }

        /// <inheritdoc />
        public int CountActive
        {
            get
            {
                lock (_lock) { return _activeSet.Count; }
            }
        }

        /// <inheritdoc />
        public int CountAll
        {
            get
            {
                lock (_lock) { return _pool.Count + _activeSet.Count; }
            }
        }

        /// <summary>
        ///     构造对象池
        /// </summary>
        /// <param name="name">池名称（调试用）</param>
        /// <param name="createFunc">对象工厂方法（不能为 null）</param>
        /// <param name="actionOnGet">获取时回调（可选，IPoolable 会自动调用）</param>
        /// <param name="actionOnReturn">归还时回调（可选，IPoolable 会自动调用）</param>
        /// <param name="actionOnDestroy">销毁时回调（可选）</param>
        /// <param name="defaultCapacity">内部容器初始容量</param>
        /// <param name="maxSize">最大容量（0=不限，默认 1000）</param>
        public ObjectPool(
            string name,
            Func<T> createFunc,
            Action<T> actionOnGet = null,
            Action<T> actionOnReturn = null,
            Action<T> actionOnDestroy = null,
            int defaultCapacity = 10,
            int maxSize = 0)
        {
            Name = name ?? typeof(T).Name;
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _actionOnGet = actionOnGet;
            _actionOnReturn = actionOnReturn;
            _actionOnDestroy = actionOnDestroy;
            _maxSize = maxSize > 0 ? maxSize : DefaultMaxSize;
            _pool = new Stack<T>(defaultCapacity);
            _activeSet = new HashSet<T>();
        }

        /// <inheritdoc />
        public T Get()
        {
            lock (_lock)
            {
                T item;

                // 优先从空闲栈取出
                if (_pool.Count > 0)
                {
                    item = _pool.Pop();
                }
                else
                {
                    // 空池：创建新对象
                    item = _createFunc();
                }

                _activeSet.Add(item);
                InvokeOnGet(item);
                return item;
            }
        }

        /// <inheritdoc />
        public PoolHandle<T> Get(out T item)
        {
            item = Get();
            return new PoolHandle<T>(item, this);
        }

        /// <inheritdoc />
        public void Return(T item)
        {
            if (item == null) return;

            lock (_lock)
            {
                // 防止重复归还
                if (!_activeSet.Remove(item)) return;

                InvokeOnReturn(item);

                // 不超过最大容量则入栈
                if (_pool.Count < _maxSize)
                {
                    _pool.Push(item);
                }
                else
                {
                    // 超出容量，销毁
                    _actionOnDestroy?.Invoke(item);
                }
            }
        }

        /// <inheritdoc />
        public void ReturnAll()
        {
            lock (_lock)
            {
                foreach (var item in _activeSet)
                {
                    InvokeOnReturn(item);

                    if (_pool.Count < _maxSize)
                    {
                        _pool.Push(item);
                    }
                    else
                    {
                        _actionOnDestroy?.Invoke(item);
                    }
                }

                _activeSet.Clear();
            }
        }

        /// <inheritdoc />
        public void Prewarm(int count)
        {
            lock (_lock)
            {
                for (int i = 0; i < count; i++)
                {
                    if (_pool.Count >= _maxSize) break;

                    var item = _createFunc();
                    InvokeOnReturn(item);
                    _pool.Push(item);
                }
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            lock (_lock)
            {
                while (_pool.Count > 0)
                {
                    var item = _pool.Pop();
                    _actionOnDestroy?.Invoke(item);
                }
            }
        }

        /// <inheritdoc />
        public void ShrinkTo(int capacity)
        {
            lock (_lock)
            {
                while (_pool.Count > capacity)
                {
                    var item = _pool.Pop();
                    _actionOnDestroy?.Invoke(item);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                // 销毁所有对象（活跃 + 空闲）
                foreach (var item in _activeSet)
                {
                    _actionOnDestroy?.Invoke(item);
                }

                while (_pool.Count > 0)
                {
                    var item = _pool.Pop();
                    _actionOnDestroy?.Invoke(item);
                }

                _activeSet.Clear();
                _pool.Clear();
            }
        }

        /// <summary>
        ///     获取对象时触发回调（含 IPoolable 自动调用）
        /// </summary>
        private void InvokeOnGet(T item)
        {
            if (item is IPoolable poolable)
            {
                poolable.OnGet();
            }

            _actionOnGet?.Invoke(item);
        }

        /// <summary>
        ///     归还对象时触发回调（含 IPoolable 自动调用）
        /// </summary>
        private void InvokeOnReturn(T item)
        {
            if (item is IPoolable poolable)
            {
                poolable.OnReturn();
            }

            _actionOnReturn?.Invoke(item);
        }
    }
}
