using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CFramework
{
    /// <summary>
    ///     事件订阅扫描与绑定工具
    ///     <para>扫描 [EventSubscribe] 标记的方法，自动创建委托并订阅到 IEventBus</para>
    /// </summary>
    public static class EventSubscriberHelper
    {
        /// <summary>
        ///     订阅信息缓存（Type -> 订阅信息列表）
        /// </summary>
        private static readonly ConcurrentDictionary<Type, List<SubscribeInfo>> _cache = new();

        /// <summary>
        ///     IEventBus.Subscribe 泛型方法定义缓存（类级别，仅初始化一次）
        /// </summary>
        private static readonly MethodInfo SubscribeMethodBase = typeof(IEventBus)
            .GetMethods()
            .First(m => m.Name == nameof(IEventBus.Subscribe)
                        && m.IsGenericMethod
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType == typeof(int));

        /// <summary>
        ///     扫描订阅者类型的 [EventSubscribe] 方法并自动订阅
        /// </summary>
        public static void AutoSubscribe(IEventSubscriber subscriber, IEventBus eventBus)
        {
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            var subscribeInfos = GetOrBuildSubscribeInfos(subscriber.GetType());

            foreach (var info in subscribeInfos)
            {
                var delegateType = typeof(Action<>).MakeGenericType(info.EventType);
                var callback = Delegate.CreateDelegate(delegateType, subscriber, info.Method);

                var subscribeMethod = SubscribeMethodBase.MakeGenericMethod(info.EventType);
                var disposable =
                    (IDisposable)subscribeMethod.Invoke(eventBus, new object[] { callback, info.Priority });
                subscriber.EventSubscriptions.Add(disposable);
            }
        }

        /// <summary>
        ///     获取或构建类型的订阅信息（带缓存）
        /// </summary>
        private static List<SubscribeInfo> GetOrBuildSubscribeInfos(Type type)
        {
            return _cache.GetOrAdd(type, BuildSubscribeInfos);
        }

        /// <summary>
        ///     扫描类型中所有 [EventSubscribe] 标记的方法
        /// </summary>
        private static List<SubscribeInfo> BuildSubscribeInfos(Type type)
        {
            var result = new List<SubscribeInfo>();
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                               BindingFlags.FlattenHierarchy;

            var methods = type.GetMethods(bindingFlags);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<EventSubscribeAttribute>();
                if (attr == null) continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                    throw new InvalidOperationException(
                        $"[EventSubscribe] 标记的方法 '{type.Name}.{method.Name}' 参数数量错误，" +
                        $"期望 1 个参数，实际 {parameters.Length} 个");

                var eventType = parameters[0].ParameterType;
                if (!typeof(IEvent).IsAssignableFrom(eventType))
                    throw new InvalidOperationException(
                        $"[EventSubscribe] 标记的方法 '{type.Name}.{method.Name}' 参数类型 " +
                        $"'{eventType.Name}' 未实现 IEvent 接口");

                result.Add(new SubscribeInfo
                {
                    Method = method,
                    EventType = eventType,
                    Priority = attr.Priority
                });
            }

            return result;
        }

        /// <summary>
        ///     订阅元信息
        /// </summary>
        private sealed class SubscribeInfo
        {
            public MethodInfo Method { get; set; }
            public Type EventType { get; set; }
            public int Priority { get; set; }
        }
    }
}