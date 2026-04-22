using CFramework.Runtime.UI;
using VContainer;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     框架模块服务安装器
    ///     <para>注册框架提供的功能模块：资源、UI、音频、场景、存档、配置</para>
    ///     <para>配置服务（IConfigService）需由游戏项目通过 LubanConfigService 子类自行注册</para>
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
            builder.InstallModule<ISaveService, SaveService>();
            builder.InstallModule<ILubanDataLoader, AddressablesLubanDataLoader>();
        }
    }
}