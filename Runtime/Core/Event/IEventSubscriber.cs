using R3;

namespace CFramework
{
    /// <summary>
    ///     事件订阅者接口
    ///     <para>实现此接口的服务可通过 [EventSubscribe] 特性标记方法实现自动订阅/取消订阅</para>
    ///     <para>注册时使用 builder.RegisterEventSubscriber&lt;T&gt;() 扩展方法</para>
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        ///     事件订阅集合，由 EventSubscriberHelper 自动管理
        /// </summary>
        CompositeDisposable EventSubscriptions { get; }
    }
}