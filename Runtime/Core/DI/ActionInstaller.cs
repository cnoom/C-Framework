using System;
using VContainer;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     委托式安装器，通过 Action 快速创建 IInstaller
    ///     <para>适用于少量服务注册，无需创建独立的 Installer 类</para>
    /// </summary>
    public sealed class ActionInstaller : IInstaller
    {
        private readonly Action<IContainerBuilder> _installAction;

        /// <summary>
        ///     创建委托式安装器
        /// </summary>
        /// <param name="installAction">注册动作</param>
        public ActionInstaller(Action<IContainerBuilder> installAction)
        {
            _installAction = installAction ?? throw new ArgumentNullException(nameof(installAction));
        }

        /// <summary>
        ///     执行注册动作
        /// </summary>
        public void Install(IContainerBuilder builder)
        {
            _installAction(builder);
        }
    }
}