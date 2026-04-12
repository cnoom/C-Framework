namespace CFramework
{
    /// <summary>
    ///     日志级别
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        ///     调试信息，仅在开发模式下输出
        /// </summary>
        Debug = 0,

        /// <summary>
        ///     一般信息
        /// </summary>
        Info = 1,

        /// <summary>
        ///     警告信息
        /// </summary>
        Warning = 2,

        /// <summary>
        ///     错误信息
        /// </summary>
        Error = 3,

        /// <summary>
        ///     异常信息
        /// </summary>
        Exception = 4,

        /// <summary>
        ///     禁用所有日志
        /// </summary>
        None = 100
    }
}