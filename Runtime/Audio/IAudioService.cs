#if CFRAMEWORK_AUDIO
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CFramework
{
    /// <summary>
    ///     音频服务接口 —— 数据驱动，基于 AudioMixer 动态解析
    ///     <para>分组寻址通过 Group 路径字符串（如 "Master/BGM"），与用户生成的 AudioGroup 枚举类型解耦</para>
    ///     <para>需要定义 CFRAMEWORK_AUDIO 编译符号才能参与编译</para>
    /// </summary>
    public interface IAudioService : IDisposable
    {
        #region 初始化

        /// <summary>
        ///     初始化音频系统
        ///     <para>使用 FrameworkSettings 中指定的 AudioMixer 自动初始化</para>
        ///     <para>解析 Group 层级 → 生成 GameObject → 挂载 AudioSource → 绑定 MixerGroup</para>
        ///     <para>解析 Snapshot 列表 → 构建快照缓存</para>
        ///     <para>解析 Exposed Parameters → 构建音量控制映射</para>
        /// </summary>
        UniTask InitializeAsync();

        #endregion

        #region 音量控制

        /// <summary>
        ///     设置分组音量（0~1 线性值，内部转 dB 操作 Mixer）
        /// </summary>
        void SetGroupVolume(string groupPath, float volume);

        /// <summary>
        ///     获取分组音量（0~1 线性值）
        /// </summary>
        float GetGroupVolume(string groupPath);

        /// <summary>
        ///     静音/取消静音
        /// </summary>
        void MuteGroup(string groupPath, bool mute);

        /// <summary>
        ///     是否已静音
        /// </summary>
        bool IsGroupMuted(string groupPath);

        #endregion

        #region 快照系统

        /// <summary>
        ///     切换到指定快照（平滑过渡）
        /// </summary>
        UniTask TransitionToSnapshotAsync(string snapshotName, float duration = 1f);

        /// <summary>
        ///     当前快照名称
        /// </summary>
        string CurrentSnapshot { get; }

        /// <summary>
        ///     获取所有可用快照名称
        /// </summary>
        IReadOnlyList<string> GetSnapshotNames();

        #endregion

        #region 播放控制

        /// <summary>
        ///     在指定分组播放音频
        ///     <para>groupPath: Mixer Group 路径，如 "Master/BGM"</para>
        ///     <para>clipKey: Addressable 资源 Key</para>
        ///     <para>options: 播放选项（音量/循环/渐入/3D等）</para>
        /// </summary>
        UniTask<AudioSourceSlot> PlayAsync(string groupPath, string clipKey,
            AudioPlayOptions options = default, CancellationToken ct = default);

        /// <summary>
        ///     停止指定 Slot
        ///     <para>slotIndex: 要停止的 Slot 索引，-1=停止最后一个活跃 Slot</para>
        /// </summary>
        void Stop(string groupPath, int slotIndex = -1, float fadeOut = 0f);

        /// <summary>
        ///     停止分组内所有播放
        /// </summary>
        void StopAll(string groupPath, float fadeOut = 0f);

        /// <summary>
        ///     交叉淡入淡出（同组内，淡出新音频 + 淡出旧循环音频）
        /// </summary>
        UniTask CrossFadeAsync(string groupPath, string newClipKey,
            float duration = 1f, AudioPlayOptions options = default,
            CancellationToken ct = default);

        #endregion

        #region 暂停/恢复

        /// <summary>
        ///     暂停所有音频
        /// </summary>
        void PauseAll();

        /// <summary>
        ///     恢复所有暂停的音频
        /// </summary>
        void ResumeAll();

        #endregion

        #region 查询

        /// <summary>
        ///     获取所有已注册的 Group 路径
        /// </summary>
        IReadOnlyList<string> GetAllGroupPaths();

        /// <summary>
        ///     是否存在指定分组
        /// </summary>
        bool HasGroup(string groupPath);

        /// <summary>
        ///     获取指定分组的 Slot 信息（用于调试/显示）
        /// </summary>
        AudioGroupInfo GetGroupInfo(string groupPath);

        #endregion

        #region 持久化

        /// <summary>
        ///     保存音量设置到 PlayerPrefs
        /// </summary>
        void SaveVolumes();

        #endregion
    }
}
#endif
