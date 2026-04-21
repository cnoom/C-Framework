#if CFRAMEWORK_AUDIO
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     播放控制器 —— 管理所有分组的音频播放/停止/淡入淡出/交叉淡入淡出
    /// </summary>
    public sealed class AudioPlaybackController
    {
        private readonly AudioMixerTree _tree;
        private readonly IAssetService _assetService;

        public AudioPlaybackController(AudioMixerTree tree, IAssetService assetService)
        {
            _tree = tree;
            _assetService = assetService;
        }

        public async UniTask<AudioSourceSlot> PlayAsync(
            string groupPath,
            string clipKey,
            AudioPlayOptions options = default,
            CancellationToken ct = default)
        {
            var node = _tree.GetNode(groupPath);
            if (node == null)
            {
                Debug.LogWarning($"[Audio] Group not found: {groupPath}");
                return null;
            }

            var handle = await _assetService.LoadAsync<AudioClip>(clipKey, ct);
            var clip = handle.As<AudioClip>();
            if (clip == null)
            {
                Debug.LogWarning($"[Audio] Failed to load clip: {clipKey}");
                return null;
            }

            var slot = options.PreferSlotIndex >= 0
                ? node.GetSlot(options.PreferSlotIndex) ?? node.AcquireSlot()
                : node.AcquireSlot();

            if (slot == null)
            {
                Debug.LogWarning($"[Audio] No available slot in group {groupPath}");
                return null;
            }

            slot.Source.clip = clip;
            slot.Source.volume = options.FadeIn > 0 ? 0f : options.Volume;
            slot.Source.pitch = options.Pitch;
            slot.Source.loop = options.Loop;
            slot.Source.spatialBlend = options.Spatial ? 1f : 0f;
            if (options.Spatial) slot.Source.transform.position = options.Position;
            slot.SetClipKey(clipKey);
            slot.SetClipHandle(handle);

            if (options.Loop)
                slot.Source.Play();
            else
                slot.Source.PlayOneShot(clip, options.Volume);

            if (options.FadeIn > 0)
            {
                var fadeCt = slot.GetFadeToken();
                FadeAsync(slot, 0f, options.Volume, options.FadeIn, fadeCt).Forget();
            }

            if (!options.Loop)
            {
                var delayMs = (int)(clip.length / options.Pitch * 1000) + 100;
                UniTask.Delay(delayMs, cancellationToken: ct)
                    .ContinueWith(() =>
                    {
                        if (slot.IsPlaying) return;
                        node.ReleaseSlot(slot.Index);
                    })
                    .Forget();
            }

            return slot;
        }

        public void Stop(string groupPath, int slotIndex, float fadeOut = 0f)
        {
            var node = _tree.GetNode(groupPath);
            if (node == null) return;

            var slot = node.GetSlot(slotIndex);
            if (slot == null || !slot.IsPlaying) return;

            if (fadeOut > 0)
            {
                var fadeCt = slot.GetFadeToken();
                FadeAsync(slot, slot.Source.volume, 0f, fadeOut, fadeCt)
                    .ContinueWith(() =>
                    {
                        slot.Source.Stop();
                        node.ReleaseSlot(slot.Index);
                    })
                    .Forget();
            }
            else
            {
                slot.Source.Stop();
                node.ReleaseSlot(slot.Index);
            }
        }

        public void StopLast(string groupPath, float fadeOut = 0f)
        {
            var node = _tree.GetNode(groupPath);
            if (node == null) return;

            var slots = node.GetAllSlots();
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i] != null && slots[i].IsPlaying)
                {
                    Stop(groupPath, slots[i].Index, fadeOut);
                    return;
                }
            }
        }

        public void StopAll(string groupPath, float fadeOut = 0f)
        {
            var node = _tree.GetNode(groupPath);
            if (node == null) return;

            var slots = node.GetAllSlots();
            foreach (var slot in slots)
            {
                if (slot != null && slot.IsPlaying)
                    Stop(groupPath, slot.Index, fadeOut);
            }
        }

        public async UniTask CrossFadeAsync(
            string groupPath,
            string newClipKey,
            float duration = 1f,
            AudioPlayOptions options = default,
            CancellationToken ct = default)
        {
            options.FadeIn = duration;
            options.Loop = true;
            var newSlot = await PlayAsync(groupPath, newClipKey, options, ct);

            var node = _tree.GetNode(groupPath);
            if (node != null && newSlot != null)
            {
                var slots = node.GetAllSlots();
                foreach (var slot in slots)
                {
                    if (slot != null && slot.IsPlaying && slot.Index != newSlot.Index && slot.Source.loop)
                        Stop(groupPath, slot.Index, duration);
                }
            }
        }

        public List<(string path, int slotIndex)> PauseAll()
        {
            var paused = new List<(string, int)>();
            foreach (var path in _tree.GetAllPaths())
            {
                var node = _tree.GetNode(path);
                if (node == null) continue;

                foreach (var slot in node.GetAllSlots())
                {
                    if (slot != null && slot.IsPlaying)
                    {
                        slot.Source.Pause();
                        paused.Add((path, slot.Index));
                    }
                }
            }
            return paused;
        }

        public void ResumeSlots(List<(string path, int slotIndex)> pausedSlots)
        {
            foreach (var (path, index) in pausedSlots)
            {
                var node = _tree.GetNode(path);
                var slot = node?.GetSlot(index);
                if (slot != null && slot.Source.clip != null)
                    slot.Source.UnPause();
            }
        }

        private async UniTask FadeAsync(
            AudioSourceSlot slot, float from, float to, float duration, CancellationToken ct)
        {
            try
            {
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    ct.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    t = t * t * (3f - 2f * t);
                    slot.Source.volume = Mathf.Lerp(from, to, t);
                    await UniTask.Yield(ct);
                }
                slot.Source.volume = to;
            }
            catch (OperationCanceledException)
            {
                // 被新 Fade 接管时取消旧的是正常行为
            }
        }
    }
}
#endif
