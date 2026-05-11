using VContainer;

namespace CFramework
{
    /// <summary>
    ///     VContainer 容器构建扩展方法，用于注册自动事件订阅者
    /// </summary>
    public static class EventSubscriberExtensions
    {
        /// <summary>
        ///     注册一个支持自动事件订阅的服务
        ///     <para>容器构建完成后会自动扫描 [EventSubscribe] 标记的方法并订阅到 IEventBus</para>
        ///     <para>容器销毁时自动取消所有订阅</para>
        ///     <para>同时映射所有接口（含 IInitializable / ITickable 等 VContainer 生命周期接口）</para>
        /// </summary>
        /// <typeparam name="T">实现 IEventSubscriber 的服务类型</typeparam>
        /// <param name="builder">容器构建器</param>
        /// <param name="lifetime">生命周期（默认 Singleton）</param>
        /// <returns>RegistrationBuilder 以支持链式调用</returns>
        public static RegistrationBuilder RegisterEventSubscriber<T>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Singleton)
            where T : class, IEventSubscriber
        {
            var registrationBuilder = builder.Register<T>(lifetime)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.RegisterBuildCallback(resolver =>
            {
                var subscriber = resolver.Resolve<T>();
                var eventBus = resolver.Resolve<IEventBus>();
                EventSubscriberHelper.AutoSubscribe(subscriber, eventBus);
            });

            return registrationBuilder;
        }

        /// <summary>
        ///     注册一个支持自动事件订阅的服务（带接口映射）
        /// </summary>
        /// <typeparam name="TInterface">注册的接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="builder">容器构建器</param>
        /// <param name="lifetime">生命周期（默认 Singleton）</param>
        /// <returns>RegistrationBuilder 以支持链式调用</returns>
        [Preserve]
        public static RegistrationBuilder RegisterEventSubscriber<TInterface, TImplement>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Singleton)
            where TImplement : class, TInterface, IEventSubscriber
            where TInterface : class
        {
            var registrationBuilder = builder.Register<TImplement>(lifetime)
                .AsImplementedInterfaces()
                .AsSelf();

            // 同时注册为实现类型，以便回调中能 Resolve 到实例
            builder.RegisterBuildCallback(resolver =>
            {
                // 通过实现类型解析，确保拿到同一个实例
                var subscriber = (IEventSubscriber)resolver.Resolve<TImplement>();
                var eventBus = resolver.Resolve<IEventBus>();
                EventSubscriberHelper.AutoSubscribe(subscriber, eventBus);
            });

            return registrationBuilder;
        }
    }
}