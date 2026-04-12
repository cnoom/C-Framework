using System;
using System.Collections.Generic;
using R3;

namespace CFramework
{
    /// <summary>
    ///     黑板实现，支持响应式数据存储
    /// </summary>
    public sealed class Blackboard : IBlackboard
    {
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
            _values[compositeKey] = value;

            // 触发响应式通知
            if (_subjects.TryGetValue(compositeKey, out var subject)) ((Subject<T>)subject).OnNext(value);

            // 触发全局变化通知
            _keyChangedSubject.OnNext(new BlackboardChange(key.Name, typeof(T)));
        }

        /// <summary>
        ///     尝试获取值
        /// </summary>
        public bool TryGet<T>(BlackboardKey<T> key, out T value)
        {
            ThrowIfDisposed();

            var compositeKey = (key.Name, typeof(T));
            if (_values.TryGetValue(compositeKey, out var obj))
            {
                value = (T)obj;
                return true;
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
            return _values.ContainsKey((key.Name, typeof(T)));
        }

        /// <summary>
        ///     移除键
        /// </summary>
        public bool Remove<T>(BlackboardKey<T> key)
        {
            ThrowIfDisposed();

            var compositeKey = (key.Name, typeof(T));
            var removed = _values.Remove(compositeKey);

            if (removed)
            {
                // 触发响应式通知（发送 default 值表示已移除）
                if (_subjects.TryGetValue(compositeKey, out var subject)) ((Subject<T>)subject).OnNext(default);

                // 触发全局变化通知
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

            // 触发所有 Subject 的通知
            foreach (var kvp in _subjects)
            {
                _keyChangedSubject.OnNext(new BlackboardChange(kvp.Key.name, kvp.Key.type));
            }

            _values.Clear();
        }

        /// <summary>
        ///     观察指定键的值变化
        /// </summary>
        public Observable<T> Observe<T>(BlackboardKey<T> key)
        {
            ThrowIfDisposed();

            var compositeKey = (key.Name, typeof(T));

            if (!_subjects.TryGetValue(compositeKey, out var subject))
            {
                subject = new Subject<T>();
                _subjects[compositeKey] = subject;
            }

            return (Subject<T>)subject;
        }

        /// <summary>
        ///     释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // 释放所有 Subject
            foreach (var subject in _subjects.Values) (subject as IDisposable)?.Dispose();

            _subjects.Clear();
            _values.Clear();
            _keyChangedSubject.Dispose();
        }

        /// <summary>
        ///     检查是否已释放
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Blackboard));
        }
    }
}