using System;
using UnityEngine.Scripting;

namespace CFramework
{
    /// <summary>
    ///     标记方法为自动事件订阅处理器
    ///     <para>方法签名要求：void MethodName(T evt)，其中 T 实现 IEvent</para>
    ///     <para>需配合 IEventSubscriber 接口和 RegisterEventSubscriber 扩展方法使用</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EventSubscribeAttribute : PreserveAttribute
    {
        public EventSubscribeAttribute(int priority = 0)
        {
            Priority = priority;
        }

        /// <summary>
        ///     订阅优先级，值越大越先执行
        /// </summary>
        public int Priority { get; }
    }
}