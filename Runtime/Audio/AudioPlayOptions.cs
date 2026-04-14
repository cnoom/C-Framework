#if CFRAMEWORK_AUDIO
using System;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     音频播放选项
    ///     <para>统一结构体控制音量/音调/循环/渐入/3D空间/指定Slot</para>
    /// </summary>
    [Serializable]
    public struct AudioPlayOptions
    {
        /// <summary>音量 0~1</summary>
        public float Volume;

        /// <summary>音调/速度（1=正常）</summary>
        public float Pitch;

        /// <summary>是否循环播放</summary>
        public bool Loop;

        /// <summary>渐入时长（秒），0=无渐入</summary>
        public float FadeIn;

        /// <summary>是否3D空间音效</summary>
        public bool Spatial;

        /// <summary>3D位置（Spatial=true 时生效）</summary>
        public Vector3 Position;

        /// <summary>指定 Slot 索引，-1=自动分配</summary>
        public int PreferSlotIndex;

        /// <summary>默认播放选项</summary>
        public static AudioPlayOptions Default => new()
        {
            Volume = 1f,
            Pitch = 1f,
            Loop = false,
            FadeIn = 0f,
            Spatial = false,
            Position = Vector3.zero,
            PreferSlotIndex = -1
        };

        /// <summary>循环 + 渐入</summary>
        public static AudioPlayOptions LoopFadeIn(float fadeIn = 1f) => new()
        {
            Volume = 1f,
            Pitch = 1f,
            Loop = true,
            FadeIn = fadeIn,
            Spatial = false,
            Position = Vector3.zero,
            PreferSlotIndex = -1
        };

        /// <summary>一次性播放（PlayOneShot）</summary>
        public static AudioPlayOptions OneShot => new()
        {
            Volume = 1f,
            Pitch = 1f,
            Loop = false,
            FadeIn = 0f,
            Spatial = false,
            Position = Vector3.zero,
            PreferSlotIndex = -1
        };

        /// <summary>3D空间音效</summary>
        public static AudioPlayOptions Spatial3D(Vector3 position) => new()
        {
            Volume = 1f,
            Pitch = 1f,
            Loop = false,
            FadeIn = 0f,
            Spatial = true,
            Position = position,
            PreferSlotIndex = -1
        };
    }
}
#endif
