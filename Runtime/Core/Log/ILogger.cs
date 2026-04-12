using System;

namespace CFramework
{
    /// <summary>
    ///     日志服务接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        ///     当前日志级别
        /// </summary>
        LogLevel LogLevel { get; set; }

        /// <summary>
        ///     检查指定级别是否启用
        /// </summary>
        bool IsEnabled(LogLevel level);

        /// <summary>
        ///     输出调试日志
        /// </summary>
        void LogDebug(string message);

        /// <summary>
        ///     输出调试日志（带标签）
        /// </summary>
        void LogDebug(string tag, string message);

        /// <summary>
        ///     输出信息日志
        /// </summary>
        void LogInfo(string message);

        /// <summary>
        ///     输出信息日志（带标签）
        /// </summary>
        void LogInfo(string tag, string message);

        /// <summary>
        ///     输出警告日志
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        ///     输出警告日志（带标签）
        /// </summary>
        void LogWarning(string tag, string message);

        /// <summary>
        ///     输出错误日志
        /// </summary>
        void LogError(string message);

        /// <summary>
        ///     输出错误日志（带标签）
        /// </summary>
        void LogError(string tag, string message);

        /// <summary>
        ///     输出异常日志
        /// </summary>
        void LogException(Exception exception);

        /// <summary>
        ///     输出异常日志（带标签）
        /// </summary>
        void LogException(string tag, Exception exception);
    }
}