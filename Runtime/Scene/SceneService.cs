using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace CFramework
{
    /// <summary>
    ///     场景服务实现
    /// </summary>
    public sealed class SceneService : ISceneService, IStartable, IDisposable
    {
        private readonly Subject<float> _loadProgress = new();
        private readonly Subject<string> _sceneLoaded = new();
        private readonly Subject<string> _sceneUnloaded = new();

        public void Dispose()
        {
            _sceneLoaded.Dispose();
            _sceneUnloaded.Dispose();
            _loadProgress.Dispose();
        }

        public string CurrentScene { get; private set; }

        public ISceneTransition Transition { get; set; } = new FadeTransition();

        public Observable<string> OnSceneLoaded => _sceneLoaded;
        public Observable<string> OnSceneUnloaded => _sceneUnloaded;
        public Observable<float> OnLoadProgress => _loadProgress;

        public async UniTask LoadAsync(string sceneName, IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            // 播放进入过渡动画
            if (Transition != null) await Transition.PlayEnterAsync(ct);

            // 卸载当前叠加场景
            await UnloadAllAdditiveScenesAsync(ct);

            // 加载新场景
            var op = SceneManager.LoadSceneAsync(sceneName);

            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();

                var p = op.progress;
                progress?.Report(p);
                _loadProgress.OnNext(p);

                await UniTask.Yield(ct);
            }

            progress?.Report(1f);
            _loadProgress.OnNext(1f);

            var oldScene = CurrentScene;
            CurrentScene = sceneName;

            _sceneUnloaded.OnNext(oldScene);
            _sceneLoaded.OnNext(sceneName);

            // 播放退出过渡动画
            if (Transition != null) await Transition.PlayExitAsync(ct);
        }

        public async UniTask LoadAdditiveAsync(string sceneName, CancellationToken ct = default)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(ct);
            }

            _sceneLoaded.OnNext(sceneName);
        }

        public async UniTask UnloadAdditiveAsync(string sceneName, CancellationToken ct = default)
        {
            await UnloadAdditiveInternalAsync(sceneName, ct);
            _sceneUnloaded.OnNext(sceneName);
        }

        public void Start()
        {
            CurrentScene = SceneManager.GetActiveScene().name;
        }

        /// <summary>
        ///     内部卸载叠加场景（不触发事件）
        /// </summary>
        private async UniTask UnloadAdditiveInternalAsync(string sceneName, CancellationToken ct)
        {
            var op = SceneManager.UnloadSceneAsync(sceneName);

            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(ct);
            }
        }

        private async UniTask UnloadAllAdditiveScenesAsync(CancellationToken ct)
        {
            // 确保 CurrentScene 已初始化，避免误删所有场景
            var currentScene = CurrentScene ?? SceneManager.GetActiveScene().name;

            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name != currentScene)
                    await UnloadAdditiveInternalAsync(scene.name, ct);
            }
        }
    }
}