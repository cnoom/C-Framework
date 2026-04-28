using VContainer;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     IContainerBuilder 安装器扩展方法
    /// </summary>
    public static class InstallerExtensions
    {
        /// <summary>
        ///     使用安装器注册服务
        /// </summary>
        /// <param name="builder">容器构建器</param>
        /// <param name="installer">安装器实例</param>
        public static void Install(this IContainerBuilder builder, IInstaller installer)
        {
            if (installer == null) return;
            installer.Install(builder);
        }

        /// <summary>
        ///     快速注册模块服务（接口 -> 实现，默认单例）
        /// </summary>
        /// <typeparam name="TInterface">注册的接口类型</typeparam>
        /// <typeparam name="TImplementation">实现类型</typeparam>
        /// <param name="builder">容器构建器</param>
        /// <param name="lifetime">生命周期（默认 Singleton）</param>
        /// <returns>RegistrationBuilder 以支持链式调用</returns>
        public static RegistrationBuilder InstallModule<TInterface, TImplementation>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Singleton)
            where TImplementation : class, TInterface
            where TInterface : class
        {
            var registration = builder.RegisterEntryPoint<TImplementation>(lifetime).As<TInterface>();

            // 自动注册 IAsyncInitializable 接口，供 GameScope.InitializeAsync() 统一等待
            if (typeof(IAsyncInitializable).IsAssignableFrom(typeof(TImplementation)))
                registration.As<IAsyncInitializable>();

            return registration;
        }
    }
}