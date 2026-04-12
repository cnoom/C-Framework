using System;

namespace CFramework
{
    /// <summary>
    ///     全局异常分发器接口
    ///     统一捕获 UniTask 异步流和 R3 Observable 中的未处理异常
    /// </summary>
    public interface IExceptionDispatcher
    {
        /// <summary>
        ///     分发异常，由注册的处理器处理
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="context">上下文信息</param>
        void Dispatch(Exception exception, string context = null);

        /// <summary>
        ///     注册自定义处理器
        /// </summary>
        /// <param name="handler">处理器回调</param>
        /// <returns>用于取消注册的IDisposable</returns>
        IDisposable RegisterHandler(Action<Exception> handler);
    }
}