using CFramework.Utility.String;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     ILogger 扩展方法，提供彩色日志功能
    ///     <para>同时提供静态日志方法，供不依赖 DI 的组件（如 StateMachine）使用</para>
    /// </summary>
    public static class LogUtility
    {
        private static ILogger _staticLogger;

        /// <summary>
        ///     静态日志器实例，由 GameScope 初始化时设置
        /// </summary>
        public static ILogger Logger
        {
            get => _staticLogger;
            set => _staticLogger = value;
        }

        /// <summary>
        ///     输出静态调试日志
        /// </summary>
        public static void Debug(string tag, string message)
        {
            if (_staticLogger != null)
                _staticLogger.LogDebug(tag, message);
            else
                UnityEngine.Debug.Log($"[{tag}] {message}");
        }

        /// <summary>
        ///     输出静态信息日志
        /// </summary>
        public static void Info(string tag, string message)
        {
            if (_staticLogger != null)
                _staticLogger.LogInfo(tag, message);
            else
                UnityEngine.Debug.Log($"[{tag}] {message}");
        }

        /// <summary>
        ///     输出静态警告日志
        /// </summary>
        public static void Warning(string tag, string message)
        {
            if (_staticLogger != null)
                _staticLogger.LogWarning(tag, message);
            else
                UnityEngine.Debug.LogWarning($"[{tag}] {message}");
        }

        /// <summary>
        ///     输出静态错误日志
        /// </summary>
        public static void Error(string tag, string message)
        {
            if (_staticLogger != null)
                _staticLogger.LogError(tag, message);
            else
                UnityEngine.Debug.LogError($"[{tag}] {message}");
        }

        public static void LogWithColor(this ILogger logger, string tag, string message, Color color,
            LogLevel logLevel = LogLevel.Debug)
        {
            tag = StringRichTextUtility.Color(tag, color);
            LogWithTag(logger, tag, message, logLevel);
        }

        public static void LogWithLevelColor(this ILogger logger, string tag, string message,
            LogLevel logLevel = LogLevel.Debug)
        {
            tag = TagWithLevelColor(tag, logLevel);
            LogWithTag(logger, tag, message, logLevel);
        }

        private static void LogWithTag(ILogger logger, string tag, string message, LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    logger.LogDebug(tag, message);
                    break;
                case LogLevel.Info:
                    logger.LogInfo(tag, message);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(tag, message);
                    break;
                case LogLevel.Error:
                    logger.LogError(tag, message);
                    break;
            }
        }

        private static string TagWithLevelColor(string tag, LogLevel logLevel)
        {
            Color color;
            switch (logLevel)
            {
                case LogLevel.Debug:
                    color = Color.green;
                    break;
                case LogLevel.Info:
                    color = Color.white;
                    break;
                case LogLevel.Warning:
                    color = Color.yellow;

                    break;
                default:
                    color = Color.red;
                    break;
            }

            return StringRichTextUtility.Color(tag, color);
        }
    }
}