using System;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     Unity 日志实现
    /// </summary>
    public sealed class UnityLogger : ILogger
    {
        private readonly FrameworkSettings _settings;
        private LogLevel _logLevel = LogLevel.Debug;

        public UnityLogger(FrameworkSettings settings)
        {
            _settings = settings;
            _logLevel = settings != null ? settings.LogLevel : LogLevel.Debug;
        }

        /// <summary>
        ///     当前日志级别（同步更新 settings，支持运行时变更）
        /// </summary>
        public LogLevel LogLevel
        {
            get => _logLevel;
            set
            {
                _logLevel = value;
                if (_settings != null) _settings.LogLevel = value;
            }
        }

        /// <summary>
        ///     检查指定级别是否启用
        /// </summary>
        public bool IsEnabled(LogLevel level)
        {
            return (int)level >= (int)_logLevel;
        }

        /// <summary>
        ///     输出调试日志
        /// </summary>
        public void LogDebug(string message)
        {
            if (!IsEnabled(LogLevel.Debug)) return;
            Debug.Log(FormatMessage(null, message));
        }

        /// <summary>
        ///     输出调试日志（带标签）
        /// </summary>
        public void LogDebug(string tag, string message)
        {
            if (!IsEnabled(LogLevel.Debug)) return;
            Debug.Log(FormatMessage(tag, message));
        }

        /// <summary>
        ///     输出信息日志
        /// </summary>
        public void LogInfo(string message)
        {
            if (!IsEnabled(LogLevel.Info)) return;
            Debug.Log(FormatMessage(null, message));
        }

        /// <summary>
        ///     输出信息日志（带标签）
        /// </summary>
        public void LogInfo(string tag, string message)
        {
            if (!IsEnabled(LogLevel.Info)) return;
            Debug.Log(FormatMessage(tag, message));
        }

        /// <summary>
        ///     输出警告日志
        /// </summary>
        public void LogWarning(string message)
        {
            if (!IsEnabled(LogLevel.Warning)) return;
            Debug.LogWarning(FormatMessage(null, message));
        }

        /// <summary>
        ///     输出警告日志（带标签）
        /// </summary>
        public void LogWarning(string tag, string message)
        {
            if (!IsEnabled(LogLevel.Warning)) return;
            Debug.LogWarning(FormatMessage(tag, message));
        }

        /// <summary>
        ///     输出错误日志
        /// </summary>
        public void LogError(string message)
        {
            if (!IsEnabled(LogLevel.Error)) return;
            Debug.LogError(FormatMessage(null, message));
        }

        /// <summary>
        ///     输出错误日志（带标签）
        /// </summary>
        public void LogError(string tag, string message)
        {
            if (!IsEnabled(LogLevel.Error)) return;
            Debug.LogError(FormatMessage(tag, message));
        }

        /// <summary>
        ///     输出异常日志
        /// </summary>
        public void LogException(Exception exception)
        {
            if (!IsEnabled(LogLevel.Exception)) return;
            if (exception == null) return;
            Debug.LogException(exception);
        }

        /// <summary>
        ///     输出异常日志（带标签）
        /// </summary>
        public void LogException(string tag, Exception exception)
        {
            if (!IsEnabled(LogLevel.Exception)) return;
            if (exception == null) return;
            Debug.LogError(FormatMessage(tag, exception.ToString()));
        }

        /// <summary>
        ///     格式化消息
        /// </summary>
        private string FormatMessage(string tag, string message)
        {
            if (string.IsNullOrEmpty(tag)) return message;
            return $"[{tag}] {message}";
        }
    }
}