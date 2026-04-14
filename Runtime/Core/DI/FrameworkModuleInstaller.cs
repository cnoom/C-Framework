using CFramework.Runtime.UI;
using VContainer;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     框架模块服务安装器
    ///     <para>注册框架提供的功能模块：资源、UI、音频、场景、配置、存档</para>
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
            builder.InstallModule<IConfigService, ConfigService>();
            builder.InstallModule<ISaveService, SaveService>();
        }
    }
}