using System.Threading;
using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     场景过渡动画接口
    /// </summary>
    public interface ISceneTransition
    {
        /// <summary>
        ///     进入动画（场景加载前）
        /// </summary>
        UniTask PlayEnterAsync(CancellationToken ct = default);

        /// <summary>
        ///     退出动画（场景加载后）
        /// </summary>
        UniTask PlayExitAsync(CancellationToken ct = default);
    }
}