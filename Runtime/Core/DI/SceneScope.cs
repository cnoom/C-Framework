using VContainer;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     场景作用域，管理场景内的对象生命周期
    /// </summary>
    public class SceneScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 子类重写以注册场景特定服务
        }
    }
}