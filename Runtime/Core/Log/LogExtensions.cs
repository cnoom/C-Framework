namespace CFramework
{
    /// <summary>
    ///     日志扩展方法
    /// </summary>
    public static class LogExtensions
    {
        /// <summary>
        ///     输出调试日志（格式化）
        /// </summary>
        public static void LogDebugFormat(this ILogger logger, string format, params object[] args)
        {
            if (!logger.IsEnabled(LogLevel.Debug)) return;
            logger.LogDebug(string.Format(format, args));
        }

        /// <summary>
        ///     输出调试日志（带标签，格式化）
        /// </summary>
        public static void LogDebugFormat(this ILogger logger, string tag, string format, params object[] args)
        {
            if (!logger.IsEnabled(LogLevel.Debug)) return;
            logger.LogDebug(tag, string.Format(format, args));
        }

        /// <summary>
        ///     输出信息日志（格式化）
        /// </summary>
        public static void LogInfoFormat(this ILogger logger, string format, params object[] args)
        {
            if (!logger.IsEnabled(LogLevel.Info)) return;
            logger.LogInfo(string.Format(format, args));
        }

        /// <summary>
        ///     输出信息日志（带标签，格式化）
        /// </summary>
        public static void LogInfoFormat(this ILogger logger, string tag, string format, params object[] args)
        {
            if (!logger.IsEnabled(LogLevel.Info)) return;
            logger.LogInfo(tag, string.Format(format, args));
        }

        /// <summary>
        ///     输出警告日志（格式化）
        /// </summary>
        public static void LogWarningFormat(this ILogger logger, string format, params object[] args)
        {
            if (!logger.IsEnabled(LogLevel.Warning)) return;
            logger.LogWarning(string.Format(format, args));
        }

        /// <summary>
        ///     输出警告日志（带标签，格式化）
        /// </summary>
        public static void LogWarningFormat(this ILogger logger, string tag, string format, params object[] args)
        {
            if (!logger.IsEnabled(LogLevel.Warning)) return;
            logger.LogWarning(tag, string.Format(format, args));
        }

        /// <summary>
        ///     输出错误日志（格式化）
        /// </summary>
        public static void LogErrorFormat(this ILogger logger, string format, params object[] args)
        {
            if (!logger.IsEnabled(LogLevel.Error)) return;
            logger.LogError(string.Format(format, args));
        }

        /// <summary>
        ///     输出错误日志（带标签，格式化）
        /// </summary>
        public static void LogErrorFormat(this ILogger logger, string tag, string format, params object[] args)
        {
            if (!logger.IsEnabled(LogLevel.Error)) return;
            logger.LogError(tag, string.Format(format, args));
        }
    }
}