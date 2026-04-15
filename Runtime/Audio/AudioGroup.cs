#if CFRAMEWORK_AUDIO
using System.ComponentModel;

namespace CFramework
{
    /// <summary>
    ///     音频分组枚举
    ///     <para>值 = 路径哈希(Animator.StringToHash)，用于运行时 O(1) 查找</para>
    ///     <para>对应框架内置 AudioMixer（Prefabs/AudioMixer.mixer）的 Group 层级</para>
    /// </summary>
    public enum AudioGroup
    {
        [Description("Master")]
        Master = 715499232,

        [Description("Master/Music")]
        Master_Music = 734377894,

        [Description("Master/Effect")]
        Master_Effect = 523937844
    }
}
#endif
