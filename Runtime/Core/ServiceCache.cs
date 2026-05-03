namespace CFramework
{
    /// <summary>
    ///     服务静态缓存桥接
    ///     <para>供无法通过 DI 注入的组件（如 Serializable 数据对象、StateMachine 等）访问框架服务</para>
    ///     <para>由 GameScope 初始化时设置，销毁时清空，业务代码不应直接修改</para>
    /// </summary>
    public static class ServiceCache
    {
        private static volatile ILogger _logger;
        private static volatile IConfigService _configService;

        /// <summary>
        ///     日志服务（由 GameScope.ResolveFrameworkServices 设置）
        /// </summary>
        public static ILogger Logger
        {
            get => _logger;
            internal set => _logger = value;
        }

        /// <summary>
        ///     配置服务（由 GameScope.ResolveFrameworkServices 设置）
        /// </summary>
        public static IConfigService ConfigService
        {
            get => _configService;
            internal set => _configService = value;
        }
    }
}
