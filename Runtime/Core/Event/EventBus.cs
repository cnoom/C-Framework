using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CFramework
{
    /// <summary>
    ///     事件总线实现
    /// </summary>
    public sealed class EventBus : IEventBus, IDisposable
    {
        private bool _disposed;

        // 异步事件处理器：Type -> List<Handler>
        private readonly Dictionary<Type, List<AsyncHandler>> _asyncHandlers = new();

        private readonly object _lock = new();

        // 同步事件存储：Type -> Subject
        private readonly Dictionary<Type, object> _subjects = new();

        // 同步事件处理器（带优先级）：Type -> SortedList
        private readonly Dictionary<Type, List<SyncHandler>> _syncHandlers = new();

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                foreach (var subject in _subjects.Values) (subject as IDisposable)?.Dispose();
                _subjects.Clear();
                _syncHandlers.Clear();
                _asyncHandlers.Clear();
            }
        }

        public Action<Exception, IEvent, object> OnHandlerError { get; set; }

        #region 同步事件

        public void Publish<T>(in T evt) where T : IEvent
        {
            var type = typeof(T);

            Subject<T> subject = null;
            bool hasSubject;
            List<SyncHandler> handlers;

            // 单次锁获取，防止与 Dispose 竞态
            lock (_lock)
            {
                if (_disposed) return;
                hasSubject = _subjects.TryGetValue(type, out var s);
                if (hasSubject) subject = (Subject<T>)s;

                if (!_syncHandlers.TryGetValue(type, out handlers)) handlers = null;
                else handlers = new List<SyncHandler>(handlers);
            }

            // 1. 触发Subject（响应式订阅）
            if (hasSubject) subject.OnNext(evt);

            // 2. 触发同步处理器（已在订阅时按优先级降序排列）
            if (handlers == null) return;

            foreach (var handler in handlers)
                try
                {
                    ((Action<T>)handler.Callback)(evt);
                }
                catch (Exception ex)
                {
                    OnHandlerError?.Invoke(ex, evt, handler.Callback);
                }
        }

        public IDisposable Subscribe<T>(Action<T> handler, int priority = 0) where T : IEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);
            var syncHandler = new SyncHandler(handler, priority);

            lock (_lock)
            {
                if (_disposed) return Disposable.Empty;
                if (!_syncHandlers.TryGetValue(type, out var list))
                {
                    list = new List<SyncHandler>();
                    _syncHandlers[type] = list;
                }

                // 按优先级降序插入，维护已排序列表
                var index = list.FindIndex(h => h.Priority < priority);
                if (index < 0)
                    list.Add(syncHandler);
                else
                    list.Insert(index, syncHandler);
            }

            return Disposable.Create(() =>
            {
                lock (_lock)
                {
                    if (_syncHandlers.TryGetValue(type, out var list)) list.Remove(syncHandler);
                }
            });
        }

        public Observable<T> Receive<T>() where T : IEvent
        {
            var type = typeof(T);

            lock (_lock)
            {
                if (_disposed) return Observable.Empty<T>();
                if (!_subjects.TryGetValue(type, out var subject))
                {
                    subject = new Subject<T>();
                    _subjects[type] = subject;
                }

                return (Observable<T>)subject;
            }
        }

        #endregion

        #region 异步事件

        public async UniTask PublishAsync<T>(T evt, CancellationToken ct = default) where T : IAsyncEvent
        {
            var type = typeof(T);
            List<AsyncHandler> handlers;

            lock (_lock)
            {
                if (_disposed) return;
                if (!_asyncHandlers.TryGetValue(type, out handlers)) return; // 无订阅者
                // 复制列表以避免迭代时修改（已在订阅时按优先级降序排列）
                handlers = new List<AsyncHandler>(handlers);
            }

            // 获取超时时间
            var timeout = evt.Timeout;
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            foreach (var handler in handlers)
                try
                {
                    await ((Func<T, CancellationToken, UniTask>)handler.Callback)(evt, linkedCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    OnHandlerError?.Invoke(new TimeoutException($"Async event handler timeout: {type.Name}"), evt,
                        handler.Callback);
                }
                catch (Exception ex)
                {
                    OnHandlerError?.Invoke(ex, evt, handler.Callback);
                }
        }

        public IDisposable SubscribeAsync<T>(Func<T, CancellationToken, UniTask> handler, int priority = 0)
            where T : IAsyncEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);
            var asyncHandler = new AsyncHandler(handler, priority);

            lock (_lock)
            {
                if (_disposed) return Disposable.Empty;
                if (!_asyncHandlers.TryGetValue(type, out var list))
                {
                    list = new List<AsyncHandler>();
                    _asyncHandlers[type] = list;
                }

                // 按优先级降序插入，维护已排序列表
                var index = list.FindIndex(h => h.Priority < priority);
                if (index < 0)
                    list.Add(asyncHandler);
                else
                    list.Insert(index, asyncHandler);
            }

            return Disposable.Create(() =>
            {
                lock (_lock)
                {
                    if (_asyncHandlers.TryGetValue(type, out var list)) list.Remove(asyncHandler);
                }
            });
        }

        #endregion

        #region 内部类

        private sealed class SyncHandler
        {
            public SyncHandler(Delegate callback, int priority)
            {
                Callback = callback;
                Priority = priority;
            }

            public Delegate Callback { get; }
            public int Priority { get; }
        }

        private sealed class AsyncHandler
        {
            public AsyncHandler(Delegate callback, int priority)
            {
                Callback = callback;
                Priority = priority;
            }

            public Delegate Callback { get; }
            public int Priority { get; }
        }

        #endregion
    }
}