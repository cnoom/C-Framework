using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     日志模块配置
    /// </summary>
    [CreateAssetMenu(fileName = "LogSettings", menuName = "CFramework/Log Settings")]
    public sealed class LogSettings : ScriptableObject
    {
        private const string DefaultPath = "LogSettings";

        [Tooltip("日志级别")]
        public LogLevel LogLevel = LogLevel.Debug;

        /// <summary>
        ///     加载默认配置
        /// </summary>
        public static LogSettings LoadDefault()
        {
            var settings = Resources.Load<LogSettings>(DefaultPath);
            if (settings == null)
            {
                settings = CreateInstance<LogSettings>();
                LogUtility.Debug("CFramework",
                    $"{nameof(LogSettings)} 未在 Resources/{DefaultPath} 找到，使用默认值");
            }

            return settings;
        }
    }
}
