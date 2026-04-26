using CFramework.Runtime.UI;
using VContainer;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     框架模块服务安装器
    ///     <para>注册框架提供的功能模块：资源、UI、音频、场景、配置、存档</para>
    ///     <para>Audio 模块需要定义 CFRAMEWORK_AUDIO 编译符号</para>
    /// </summary>
    public sealed class FrameworkModuleInstaller : IInstaller
    {
        /// <summary>
        ///     安装框架模块服务
        /// </summary>
        public void Install(IContainerBuilder builder)
        {
            builder.InstallModule<IAssetService, AssetService>();
            builder.InstallModule<IUIService, UIService>();
#if CFRAMEWORK_AUDIO
            builder.InstallModule<IAudioService, AudioService>();
#endif
            builder.InstallModule<ISceneService, SceneService>();

            // 配置模块：默认使用 CompositeConfigProvider 包裹 SOConfigProvider
            // 游戏可通过 GameScope.AddInstaller 追加额外 Provider（如 JSON、Memory）
            builder.Register<IConfigProvider>(container =>
            {
                var soProvider = new SOConfigProvider(container.Resolve<IAssetService>());
                var composite = new CompositeConfigProvider(soProvider);
                return composite;
            }, Lifetime.Singleton);
            builder.InstallModule<IConfigService, ConfigService>();

            builder.Register<ISaveSerializer, NewtonsoftJsonSerializer>(Lifetime.Singleton);
            builder.InstallModule<ISaveService, SaveService>();
            builder.InstallModule<IPoolService, PoolService>();
        }
    }
}