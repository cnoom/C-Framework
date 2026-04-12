using VContainer;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     核心服务安装器
    ///     <para>注册框架核心基础服务：异常分发器、事件总线、日志系统、资源加载提供者</para>
    /// </summary>
    public sealed class CoreServiceInstaller : IInstaller
    {
        /// <summary>
        ///     安装核心服务
        /// </summary>
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IExceptionDispatcher, DefaultExceptionDispatcher>(Lifetime.Singleton);
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.Register<ILogger, UnityLogger>(Lifetime.Singleton);
            builder.Register<IAssetProvider, AddressableAssetProvider>(Lifetime.Singleton);
        }
    }
}