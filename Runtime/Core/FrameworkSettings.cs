using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     框架全局配置（组合容器）
    ///     <para>引用各模块级 Settings，各服务通过 DI 注入自己需要的模块配置</para>
    ///     <para>子 Settings 为空时自动加载默认值</para>
    /// </summary>
    [CreateAssetMenu(fileName = "FrameworkSettings", menuName = "CFramework/Settings")]
    public sealed class FrameworkSettings : ScriptableObject
    {
        public const string DefaultPath = "FrameworkSettings";

        #region 模块配置

        [Header("模块配置")]
        [Tooltip("资源模块配置")]
        public AssetSettings Asset;

        [Tooltip("UI 模块配置")]
        public UISettings UI;

        [Tooltip("音频模块配置")]
        public AudioSettings Audio;

        [Tooltip("存档模块配置")]
        public SaveSettings Save;

        [Tooltip("对象池模块配置")]
        public PoolSettings Pool;

        [Tooltip("日志模块配置")]
        public LogSettings Log;

        [Tooltip("配置表模块配置")]
        public ConfigSettings Config;

        #endregion

        /// <summary>
        ///     获取资源配置（null 时自动 fallback）
        /// </summary>
        public AssetSettings GetAssetSettings() => Asset ? Asset : AssetSettings.LoadDefault();

        /// <summary>
        ///     获取 UI 配置（null 时自动 fallback）
        /// </summary>
        public UISettings GetUISettings() => UI ? UI : UISettings.LoadDefault();

        /// <summary>
        ///     获取音频配置（null 时自动 fallback）
        /// </summary>
        public AudioSettings GetAudioSettings() => Audio ? Audio : AudioSettings.LoadDefault();

        /// <summary>
        ///     获取存档配置（null 时自动 fallback）
        /// </summary>
        public SaveSettings GetSaveSettings() => Save ? Save : SaveSettings.LoadDefault();

        /// <summary>
        ///     获取对象池配置（null 时自动 fallback）
        /// </summary>
        public PoolSettings GetPoolSettings() => Pool ? Pool : PoolSettings.LoadDefault();

        /// <summary>
        ///     获取日志配置（null 时自动 fallback）
        /// </summary>
        public LogSettings GetLogSettings() => Log ? Log : LogSettings.LoadDefault();

        /// <summary>
        ///     获取配置表配置（null 时自动 fallback）
        /// </summary>
        public ConfigSettings GetConfigSettings() => Config ? Config : ConfigSettings.LoadDefault();

        /// <summary>
        ///     加载默认设置
        /// </summary>
        public static FrameworkSettings LoadDefault()
        {
            var settings = Resources.Load<FrameworkSettings>(DefaultPath);
            if (settings == null)
            {
                settings = CreateInstance<FrameworkSettings>();
                LogUtility.Debug("CFramework",
                    $"{nameof(FrameworkSettings)} 未在 Resources/{DefaultPath} 找到，使用默认值");
            }

            return settings;
        }
    }
}