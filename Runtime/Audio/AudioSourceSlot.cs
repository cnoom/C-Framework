#if CFRAMEWORK_AUDIO
using System.Threading;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     音源槽位 —— 每个 Slot 对应一个 AudioSource 组件
    ///     <para>同一 GameObject 上可挂多个 AudioSource，按 Index 管理区分</para>
    /// </summary>
    public sealed class AudioSourceSlot
    {
        /// <summary>Slot 索引（在 GroupNode 内唯一）</summary>
        public int Index { get; }

        /// <summary>底层 AudioSource 组件</summary>
        public AudioSource Source { get; }

        /// <summary>当前是否正在播放</summary>
        public bool IsPlaying => Source != null && Source.isPlaying;

        /// <summary>当前播放的 Clip 资源 Key（调试用）</summary>
        public string CurrentClipKey { get; private set; }

        private CancellationTokenSource _fadeCts;
        private AssetHandle _clipHandle;

        public AudioSourceSlot(int index, AudioSource source)
        {
            Index = index;
            Source = source;
        }

        /// <summary>
        ///     设置当前播放的 Clip Key
        /// </summary>
        public void SetClipKey(string key) => CurrentClipKey = key;

        /// <summary>
        ///     绑定资源句柄（Slot 回收时自动 Dispose）
        /// </summary>
        public void SetClipHandle(AssetHandle handle)
        {
            // 防御性释放：先释放旧 handle
            _clipHandle.Dispose();
            _clipHandle = handle;
        }

        /// <summary>
        ///     重置 Slot 到初始状态（停止播放、清空引用、释放资源）
        /// </summary>
        public void Reset()
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = null;

            // 释放资源句柄（递减 AssetService 引用计数）
            _clipHandle.Dispose();
            _clipHandle = default;

            if (Source != null)
            {
                Source.Stop();
                Source.clip = null;
                Source.volume = 1f;
                Source.pitch = 1f;
                Source.loop = false;
                Source.spatialBlend = 0f;
                Source.mute = false;
            }

            CurrentClipKey = null;
        }

        /// <summary>
        ///     获取淡入淡出的 CancellationToken（会取消上一次未完成的淡入淡出）
        /// </summary>
        public CancellationToken GetFadeToken()
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = new CancellationTokenSource();
            return _fadeCts.Token;
        }
    }
}
#endif
