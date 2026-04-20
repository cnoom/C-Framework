using CFramework.Utility.String;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     ILogger 扩展方法，提供彩色日志功能
    /// </summary>
    public static class LogUtility
    {
        public static void LogWithColor(this ILogger logger, string tag, string message, Color color,
            LogLevel logLevel = LogLevel.Debug)
        {
            tag = StringRichTextUtility.Color(tag, color);
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

        public static void LogWithLevelColor(this ILogger logger, string tag, string message,
            LogLevel logLevel = LogLevel.Debug)
        {
            tag = TagWithLevelColor(tag, logLevel);
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