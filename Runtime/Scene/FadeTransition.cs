using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CFramework
{
    /// <summary>
    ///     淡入淡出过渡动画
    /// </summary>
    public sealed class FadeTransition : ISceneTransition
    {
        private Canvas _canvas;
        private Image _overlay;
        public float Duration { get; set; } = 0.5f;
        public Color FadeColor { get; set; } = Color.black;

        public async UniTask PlayEnterAsync(CancellationToken ct = default)
        {
            CreateOverlay();
            _overlay.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, 0f);

            await FadeAsync(0f, 1f, ct);
        }

        public async UniTask PlayExitAsync(CancellationToken ct = default)
        {
            await FadeAsync(1f, 0f, ct);
            DestroyOverlay();
        }

        private void CreateOverlay()
        {
            if (_canvas != null) return;

            var go = new GameObject("[FadeTransition]");
            Object.DontDestroyOnLoad(go);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            _overlay = go.AddComponent<Image>();
            _overlay.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, 0f);
        }

        private void DestroyOverlay()
        {
            if (_canvas != null)
            {
                Object.Destroy(_canvas.gameObject);
                _canvas = null;
                _overlay = null;
            }
        }

        private async UniTask FadeAsync(float from, float to, CancellationToken ct)
        {
            if (_overlay == null) return;

            var elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / Duration);
                var alpha = Mathf.Lerp(from, to, t);
                _overlay.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, alpha);
                await UniTask.Yield(ct);
            }

            _overlay.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, to);
        }
    }
}