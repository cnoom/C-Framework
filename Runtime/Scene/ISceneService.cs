using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CFramework
{
    /// <summary>
    ///     场景服务接口
    /// </summary>
    public interface ISceneService
    {
        /// <summary>
        ///     过渡动画
        /// </summary>
        ISceneTransition Transition { get; set; }

        /// <summary>
        ///     当前场景名称
        /// </summary>
        string CurrentScene { get; }

        /// <summary>
        ///     场景加载完成事件
        /// </summary>
        Observable<string> OnSceneLoaded { get; }

        /// <summary>
        ///     场景卸载完成事件
        /// </summary>
        Observable<string> OnSceneUnloaded { get; }

        /// <summary>
        ///     加载进度事件
        /// </summary>
        Observable<float> OnLoadProgress { get; }

        /// <summary>
        ///     加载场景
        /// </summary>
        UniTask LoadAsync(string sceneName, IProgress<float> progress = null, CancellationToken ct = default);

        /// <summary>
        ///     加载叠加场景
        /// </summary>
        UniTask LoadAdditiveAsync(string sceneName, CancellationToken ct = default);

        /// <summary>
        ///     卸载叠加场景
        /// </summary>
        UniTask UnloadAdditiveAsync(string sceneName, CancellationToken ct = default);
    }
}