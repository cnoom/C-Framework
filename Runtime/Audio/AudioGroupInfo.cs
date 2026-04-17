#if CFRAMEWORK_AUDIO
namespace CFramework
{
    /// <summary>
    ///     分组调试信息（只读快照）
    /// </summary>
    public struct AudioGroupInfo
    {
        /// <summary>Mixer Group 路径</summary>
        public string Path;

        /// <summary>Slot 总数</summary>
        public int TotalSlots;

        /// <summary>活跃 Slot 数</summary>
        public int ActiveSlots;

        /// <summary>当前音量（0~1 线性值）</summary>
        public float Volume;

        /// <summary>是否静音</summary>
        public bool IsMuted;
    }
}
#endif
