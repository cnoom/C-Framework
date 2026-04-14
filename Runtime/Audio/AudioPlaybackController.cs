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

        /// <summary>
        ///     在指定分组播放音频
        /// </summary>
        public async UniTask<AudioSourceSlot> PlayAsync(
            AudioGroup group,
            string clipKey,
            AudioPlayOptions options = default,
            CancellationToken ct = default)
        {
            var node = _tree.GetNode(group);
            if (node == null)
            {
                Debug.LogWarning($"[Audio] Group not found: {group}");
                return null;
            }

            // 加载音频资源
            var handle = await _assetService.LoadAsync<AudioClip>(clipKey, ct);
            var clip = handle.As<AudioClip>();
            if (clip == null)
            {
                Debug.LogWarning($"[Audio] Failed to load clip: {clipKey}");
                return null;
            }

            // 获取 Slot
            var slot = options.PreferSlotIndex >= 0
                ? node.GetSlot(options.PreferSlotIndex) ?? node.AcquireSlot()
                : node.AcquireSlot();

            if (slot == null)
            {
                Debug.LogWarning($"[Audio] No available slot in group {group}");
                return null;
            }

            // 配置 AudioSource
            slot.Source.clip = clip;
            slot.Source.volume = options.FadeIn > 0 ? 0f : options.Volume;
            slot.Source.pitch = options.Pitch;
            slot.Source.loop = options.Loop;
            slot.Source.spatialBlend = options.Spatial ? 1f : 0f;
            if (options.Spatial) slot.Source.transform.position = options.Position;
            slot.SetClipKey(clipKey);

            // 播放
            if (options.Loop)
            {
                slot.Source.Play();
            }
            else
            {
                slot.Source.PlayOneShot(clip, options.Volume);
            }

            // 渐入
            if (options.FadeIn > 0)
            {
                var fadeCt = slot.GetFadeToken();
                FadeAsync(slot, 0f, options.Volume, options.FadeIn, fadeCt).Forget();
            }

            // 一次性播放：播放完毕后自动回收 Slot
            if (!options.Loop)
            {
                var delayMs = (int)(clip.length / options.Pitch * 1000) + 100;
                UniTask.Delay(delayMs, cancellationToken: ct)
                    .ContinueWith(() =>
                    {
                        if (slot.IsPlaying) return; // 如果已经被手动停止则跳过
                        node.ReleaseSlot(slot.Index);
                    })
                    .Forget();
            }

            return slot;
        }

        /// <summary>
        ///     停止指定 Slot
        /// </summary>
        public void Stop(AudioGroup group, int slotIndex, float fadeOut = 0f)
        {
            var node = _tree.GetNode(group);
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

        /// <summary>
        ///     停止最后一个活跃 Slot（slotIndex=-1 时的语义）
        /// </summary>
        public void StopLast(AudioGroup group, float fadeOut = 0f)
        {
            var node = _tree.GetNode(group);
            if (node == null) return;

            // 从后往前找第一个正在播放的 Slot
            var slots = node.GetAllSlots();
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i] != null && slots[i].IsPlaying)
                {
                    Stop(group, slots[i].Index, fadeOut);
                    return;
                }
            }
        }

        /// <summary>
        ///     停止分组内所有播放
        /// </summary>
        public void StopAll(AudioGroup group, float fadeOut = 0f)
        {
            var node = _tree.GetNode(group);
            if (node == null) return;

            // 复制一份避免迭代中修改集合
            var slots = node.GetAllSlots();
            foreach (var slot in slots)
            {
                if (slot != null && slot.IsPlaying)
                    Stop(group, slot.Index, fadeOut);
            }
        }

        /// <summary>
        ///     交叉淡入淡出（同组内，淡出新音频 + 淡出旧循环音频）
        /// </summary>
        public async UniTask CrossFadeAsync(
            AudioGroup group,
            string newClipKey,
            float duration = 1f,
            AudioPlayOptions options = default,
            CancellationToken ct = default)
        {
            // 播放新音频到新 Slot
            options.FadeIn = duration;
            options.Loop = true;
            var newSlot = await PlayAsync(group, newClipKey, options, ct);

            // 淡出同组内其他正在播放的循环 Slot
            var node = _tree.GetNode(group);
            if (node != null && newSlot != null)
            {
                var slots = node.GetAllSlots();
                foreach (var slot in slots)
                {
                    if (slot != null && slot.IsPlaying && slot.Index != newSlot.Index && slot.Source.loop)
                        Stop(group, slot.Index, duration);
                }
            }
        }

        /// <summary>
        ///     暂停指定分组的所有播放
        /// </summary>
        public void PauseGroup(AudioGroup group)
        {
            var node = _tree.GetNode(group);
            if (node == null) return;

            foreach (var slot in node.GetAllSlots())
            {
                if (slot != null && slot.IsPlaying)
                    slot.Source.Pause();
            }
        }

        /// <summary>
        ///     恢复指定分组的所有播放
        /// </summary>
        public void ResumeGroup(AudioGroup group)
        {
            var node = _tree.GetNode(group);
            if (node == null) return;

            foreach (var slot in node.GetAllSlots())
            {
                if (slot != null && !slot.IsPlaying && slot.Source.clip != null)
                    slot.Source.UnPause();
            }
        }

        /// <summary>
        ///     暂停所有分组
        /// </summary>
        public List<(AudioGroup group, int slotIndex)> PauseAll()
        {
            var paused = new List<(AudioGroup, int)>();
            foreach (var group in _tree.GetAllGroups())
            {
                var node = _tree.GetNode(group);
                if (node == null) continue;

                foreach (var slot in node.GetAllSlots())
                {
                    if (slot != null && slot.IsPlaying)
                    {
                        slot.Source.Pause();
                        paused.Add((group, slot.Index));
                    }
                }
            }
            return paused;
        }

        /// <summary>
        ///     恢复指定的 Slot 列表
        /// </summary>
        public void ResumeSlots(List<(AudioGroup group, int slotIndex)> pausedSlots)
        {
            foreach (var (group, index) in pausedSlots)
            {
                var node = _tree.GetNode(group);
                var slot = node?.GetSlot(index);
                if (slot != null && slot.Source.clip != null)
                    slot.Source.UnPause();
            }
        }

        /// <summary>
        ///     渐入渐出核心
        /// </summary>
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
                    // SmoothStep 让过渡更自然
                    t = t * t * (3f - 2f * t);
                    slot.Source.volume = Mathf.Lerp(from, to, t);
                    await UniTask.Yield(ct);
                }
                slot.Source.volume = to;
            }
            catch (OperationCanceledException)
            {
                // 淡入淡出被取消是正常行为（新 Fade 接管时取消旧的）
            }
        }
    }
}
#endif
