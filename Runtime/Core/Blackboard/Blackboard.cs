using System;
using System.Collections.Generic;
using System.Threading;
using R3;

namespace CFramework
{
    /// <summary>
    ///     黑板实现，支持响应式数据存储
    ///     <para>使用 ReaderWriterLockSlim 保护并发读写</para>
    /// </summary>
    public sealed class Blackboard : IBlackboard
    {
        /// <summary>
        ///     读写锁，保护并发访问
        /// </summary>
        private readonly ReaderWriterLockSlim _lock = new();

        /// <summary>
        ///     全局键变化事件
        /// </summary>
        private readonly Subject<BlackboardChange> _keyChangedSubject = new();

        /// <summary>
        ///     响应式订阅：复合键 -> Subject
        /// </summary>
        private readonly Dictionary<(string name, Type type), object> _subjects = new();

        /// <summary>
        ///     值存储：复合键（键名+类型）-> 值
        /// </summary>
        private readonly Dictionary<(string name, Type type), object> _values = new();

        /// <summary>
        ///     资源释放标记
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///     任意键变化事件
        /// </summary>
        public Observable<BlackboardChange> OnKeyChanged => _keyChangedSubject;

        /// <summary>
        ///     设置值
        /// </summary>
        public void Set<T>(BlackboardKey<T> key, T value)
        {
            ThrowIfDisposed();

            var compositeKey = (key.Name, typeof(T));

            _lock.EnterWriteLock();
            try
            {
                _values[compositeKey] = value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            // 通知在锁外执行，避免在锁内触发订阅者回调导致死锁
            NotifyValueChanged(compositeKey, value, key.Name);
        }

        /// <summary>
        ///     尝试获取值
        /// </summary>
        public bool TryGet<T>(BlackboardKey<T> key, out T value)
        {
            ThrowIfDisposed();

            var compositeKey = (key.Name, typeof(T));
            _lock.EnterReadLock();
            try
            {
                if (_values.TryGetValue(compositeKey, out var obj))
                {
                    value = (T)obj;
                    return true;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            value = default;
            return false;
        }

        /// <summary>
        ///     获取值，如果不存在则返回默认值
        /// </summary>
        public T Get<T>(BlackboardKey<T> key, T defaultValue = default)
        {
            return TryGet(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        ///     检查键是否存在
        /// </summary>
        public bool Has<T>(BlackboardKey<T> key)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                return _values.ContainsKey((key.Name, typeof(T)));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        ///     移除键
        /// </summary>
        public bool Remove<T>(BlackboardKey<T> key)
        {
            ThrowIfDisposed();

            var compositeKey = (key.Name, typeof(T));
            bool removed;

            _lock.EnterWriteLock();
            try
            {
                removed = _values.Remove(compositeKey);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            if (removed)
            {
                // 在锁外触发通知，与 Set<T> 和 Clear() 保持一致
                Subject<T> subject = null;
                _lock.EnterReadLock();
                try
                {
                    _subjects.TryGetValue(compositeKey, out var s);
                    subject = (Subject<T>)s;
                }
                finally
                {
                    _lock.ExitReadLock();
                }

                // 触发响应式通知（发送 default 值表示已移除）
                subject?.OnNext(default);
                _keyChangedSubject.OnNext(new BlackboardChange(key.Name, typeof(T)));
            }

            return removed;
        }

        /// <summary>
        ///     清空所有数据
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            List<(string name, Type type)> keysToNotify = null;

            _lock.EnterWriteLock();
            try
            {
                if (_subjects.Count > 0)
                {
                    keysToNotify = new List<(string, Type)>(_subjects.Count);
                    foreach (var kvp in _subjects)
                        keysToNotify.Add(kvp.Key);
                }

                _values.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            // 在锁外触发通知，与 Set<T> 保持一致，避免在锁内触发订阅者回调导致死锁
            if (keysToNotify != null)
                foreach (var key in keysToNotify)
                    _keyChangedSubject.OnNext(new BlackboardChange(key.name, key.type));
        }

        /// <summary>
        ///     观察指定键的值变化
        /// </summary>
        public Observable<T> Observe<T>(BlackboardKey<T> key)
        {
            ThrowIfDisposed();

            var compositeKey = (key.Name, typeof(T));

            _lock.EnterWriteLock();
            try
            {
                if (!_subjects.TryGetValue(compositeKey, out var subject))
                {
                    subject = new Subject<T>();
                    _subjects[compositeKey] = subject;
                }

                return (Subject<T>)subject;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _lock.EnterWriteLock();
            try
            {
                // 释放所有 Subject
                foreach (var subject in _subjects.Values)
                    (subject as IDisposable)?.Dispose();

                _subjects.Clear();
                _values.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            _keyChangedSubject.Dispose();
            _lock.Dispose();
        }

        /// <summary>
        ///     检查是否已释放
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Blackboard));
        }

        /// <summary>
        ///     通知值变化（在锁外调用，避免死锁）
        /// </summary>
        private void NotifyValueChanged<T>((string name, Type type) compositeKey, T value, string keyName)
        {
            Subject<T> subject = null;
            _lock.EnterReadLock();
            try
            {
                if (_subjects.TryGetValue(compositeKey, out var s))
                    subject = (Subject<T>)s;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            subject?.OnNext(value);
            _keyChangedSubject.OnNext(new BlackboardChange(keyName, typeof(T)));
        }
    }
}
