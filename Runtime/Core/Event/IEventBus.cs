using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CFramework
{
    /// <summary>
    ///     事件总线接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        ///     处理器异常回调（支持多个订阅者，外部只能添加/移除，不能覆盖）
        /// </summary>
        event Action<Exception, IEvent, object> OnHandlerError;

        /// <summary>
        ///     同步发布事件
        /// </summary>
        void Publish<T>(in T evt) where T : IEvent;

        /// <summary>
        ///     异步发布事件
        /// </summary>
        UniTask PublishAsync<T>(T evt, CancellationToken ct = default) where T : IAsyncEvent;

        /// <summary>
        ///     订阅事件（优先级在订阅时指定，值越大越先执行）
        /// </summary>
        IDisposable Subscribe<T>(Action<T> handler, int priority = 0) where T : IEvent;

        /// <summary>
        ///     异步订阅事件
        /// </summary>
        IDisposable SubscribeAsync<T>(Func<T, CancellationToken, UniTask> handler, int priority = 0)
            where T : IAsyncEvent;

        /// <summary>
        ///     响应式订阅事件
        /// </summary>
        Observable<T> Receive<T>() where T : IEvent;
    }
}