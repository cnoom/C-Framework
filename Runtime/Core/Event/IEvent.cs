using System;

namespace CFramework
{
    /// <summary>
    ///     事件标记接口（支持struct/class）
    /// </summary>
    public interface IEvent
    {
    }

    /// <summary>
    ///     异步事件接口
    /// </summary>
    public interface IAsyncEvent : IEvent
    {
        /// <summary>
        ///     超时时间，默认5秒
        /// </summary>
        TimeSpan Timeout => TimeSpan.FromSeconds(5);
    }
}