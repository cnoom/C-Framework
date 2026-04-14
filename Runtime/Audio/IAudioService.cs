#if CFRAMEWORK_AUDIO
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace CFramework
{
    /// <summary>
    ///     音频服务接口 —— 数据驱动，基于 AudioMixer 动态解析
    ///     <para>分组寻址通过编辑器生成的 AudioGroup 枚举，编译期安全</para>
    ///     <para>需要定义 CFRAMEWORK_AUDIO 符号才能编译（由 AudioGroupGenerator 自动定义）</para>
    /// </summary>
    public interface IAudioService : IDisposable
    {
        #region 初始化

        /// <summary>
        ///     根据 AudioMixer 初始化音频系统
        ///     <para>解析 Group 层级 → 生成 GameObject → 挂载 AudioSource → 绑定 MixerGroup</para>
        ///     <para>解析 Snapshot 列表 → 构建快照缓存</para>
        ///     <para>解析 Exposed Parameters → 构建音量控制映射</para>
        /// </summary>
        UniTask InitializeAsync(AudioMixer mixer, AudioMixerSnapshot[] snapshots = null);

        #endregion

        #region 音量控制

        /// <summary>
        ///     设置分组音量（0~1 线性值，内部转 dB 操作 Mixer）
        /// </summary>
        void SetGroupVolume(AudioGroup group, float volume);

        /// <summary>
        ///     获取分组音量（0~1 线性值）
        /// </summary>
        float GetGroupVolume(AudioGroup group);

        /// <summary>
        ///     静音/取消静音
        /// </summary>
        void MuteGroup(AudioGroup group, bool mute);

        /// <summary>
        ///     是否已静音
        /// </summary>
        bool IsGroupMuted(AudioGroup group);

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
        System.Collections.Generic.IReadOnlyList<string> GetSnapshotNames();

        #endregion

        #region 播放控制

        /// <summary>
        ///     在指定分组播放音频
        ///     <para>group: AudioGroup.Master_BGM, AudioGroup.Master_SFX 等（编译期安全）</para>
        ///     <para>clipKey: Addressable 资源 Key</para>
        ///     <para>options: 播放选项（音量/循环/渐入/3D等）</para>
        /// </summary>
        UniTask<AudioSourceSlot> PlayAsync(AudioGroup group, string clipKey,
            AudioPlayOptions options = default, CancellationToken ct = default);

        /// <summary>
        ///     停止指定 Slot
        ///     <para>slotIndex: 要停止的 Slot 索引，-1=停止最后一个活跃 Slot</para>
        /// </summary>
        void Stop(AudioGroup group, int slotIndex = -1, float fadeOut = 0f);

        /// <summary>
        ///     停止分组内所有播放
        /// </summary>
        void StopAll(AudioGroup group, float fadeOut = 0f);

        /// <summary>
        ///     交叉淡入淡出（同组内，淡出新音频 + 淡出旧循环音频）
        /// </summary>
        UniTask CrossFadeAsync(AudioGroup group, string newClipKey,
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
        ///     获取所有分组枚举值
        /// </summary>
        System.Collections.Generic.IReadOnlyList<AudioGroup> GetAllGroups();

        /// <summary>
        ///     是否存在指定分组
        /// </summary>
        bool HasGroup(AudioGroup group);

        /// <summary>
        ///     获取指定分组的 Slot 信息（用于调试/显示）
        /// </summary>
        AudioGroupInfo GetGroupInfo(AudioGroup group);

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
