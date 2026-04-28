using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     异步初始化接口
    ///     <para>实现此接口的服务会在 GameScope.InitializeAsync() 中被自动等待</para>
    ///     <para>适用于需要异步加载资源或等待外部就绪的服务（如 UIService、AudioService）</para>
    /// </summary>
    public interface IAsyncInitializable
    {
        /// <summary>
        ///     异步初始化服务
        ///     <para>在 DI 容器构建完成后由 GameScope 统一调用并等待</para>
        /// </summary>
        UniTask InitializeAsync();
    }
}
