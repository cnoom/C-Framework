using UnityEngine;
using UnityEngine.Serialization;

namespace CFramework
{
    /// <summary>
    ///     框架全局配置（组合容器）
    ///     <para>引用各模块级 Settings，各服务通过 DI 注入自己需要的模块配置</para>
    ///     <para>子 Settings 为空时自动加载默认值，兼容旧版 asset</para>
    /// </summary>
    [CreateAssetMenu(fileName = "FrameworkSettings", menuName = "CFramework/Settings")]
    public sealed class FrameworkSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        private const string DefaultPath = "FrameworkSettings";

        #region 旧字段（保留序列化兼容，运行时迁移后忽略）

        // 以下字段仅在旧版 asset 文件中存在，用于一次性自动迁移
        [FormerlySerializedAs("MemoryBudgetMB")] [SerializeField] [HideInInspector]
        private int _legacyMemoryBudgetMB;

        [FormerlySerializedAs("MaxLoadPerFrame")] [SerializeField] [HideInInspector]
        private int _legacyMaxLoadPerFrame;

        [FormerlySerializedAs("MaxNavigationStack")] [SerializeField] [HideInInspector]
        private int _legacyMaxNavigationStack;

        [FormerlySerializedAs("AutoSaveInterval")] [SerializeField] [HideInInspector]
        private int _legacyAutoSaveInterval;

        [FormerlySerializedAs("EncryptionKey")] [SerializeField] [HideInInspector]
        private string _legacyEncryptionKey;

        [FormerlySerializedAs("LogLevel")] [SerializeField] [HideInInspector]
        private int _legacyLogLevel;

        [FormerlySerializedAs("ConfigAddressPrefix")] [SerializeField] [HideInInspector]
        private string _legacyConfigAddressPrefix;

        [FormerlySerializedAs("MaxSlotsPerGroup")] [SerializeField] [HideInInspector]
        private int _legacyMaxSlotsPerGroup;

        [FormerlySerializedAs("VolumePrefsPrefix")] [SerializeField] [HideInInspector]
        private string _legacyVolumePrefsPrefix;

        [FormerlySerializedAs("GroupSlotConfig")] [SerializeField] [HideInInspector]
        private string _legacyGroupSlotConfig;

        /// <summary>
        ///     标记是否已完成旧字段迁移（非序列化，运行时状态）
        /// </summary>
        private bool _migrated;

        #endregion

        #region 模块配置（新结构）

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

        #region ISerializationCallbackReceiver - 自动迁移旧 asset

        public void OnBeforeSerialize()
        {
            // 无需操作
        }

        public void OnAfterDeserialize()
        {
            // 检测是否有旧字段数据需要迁移（旧字段有值 + 对应子 Settings 为空）
            // 仅需迁移值与默认值不同的字段
            if (_migrated) return;
            _migrated = true;

            // 检测是否存在旧数据（以非零/非空字段判断）
            bool hasLegacyData = _legacyMemoryBudgetMB != 0
                                 || _legacyMaxLoadPerFrame != 0
                                 || _legacyMaxNavigationStack != 0
                                 || !string.IsNullOrEmpty(_legacyEncryptionKey)
                                 || _legacyLogLevel != 0
                                 || !string.IsNullOrEmpty(_legacyConfigAddressPrefix)
                                 || _legacyAutoSaveInterval != 0
                                 || !string.IsNullOrEmpty(_legacyVolumePrefsPrefix)
                                 || !string.IsNullOrEmpty(_legacyGroupSlotConfig)
                                 || _legacyMaxSlotsPerGroup != 0;
            if (!hasLegacyData) return;

            // 运行时迁移：创建子 Settings 并填入旧值
            // 注意：CreateInstance 在 OnAfterDeserialize 中可安全调用
            if (Asset == null && (_legacyMemoryBudgetMB != 0 || _legacyMaxLoadPerFrame != 0))
            {
                Asset = CreateInstance<AssetSettings>();
                if (_legacyMemoryBudgetMB != 0) Asset.MemoryBudgetMB = _legacyMemoryBudgetMB;
                if (_legacyMaxLoadPerFrame != 0) Asset.MaxLoadPerFrame = _legacyMaxLoadPerFrame;
            }

            if (UI == null && _legacyMaxNavigationStack != 0)
            {
                UI = CreateInstance<UISettings>();
                if (_legacyMaxNavigationStack != 0) UI.MaxNavigationStack = _legacyMaxNavigationStack;
            }

            if (Audio == null && (_legacyMaxSlotsPerGroup != 0 || !string.IsNullOrEmpty(_legacyVolumePrefsPrefix) || !string.IsNullOrEmpty(_legacyGroupSlotConfig)))
            {
                Audio = CreateInstance<AudioSettings>();
                if (_legacyMaxSlotsPerGroup != 0) Audio.MaxSlotsPerGroup = _legacyMaxSlotsPerGroup;
                if (!string.IsNullOrEmpty(_legacyVolumePrefsPrefix)) Audio.VolumePrefsPrefix = _legacyVolumePrefsPrefix;
                if (!string.IsNullOrEmpty(_legacyGroupSlotConfig)) Audio.GroupSlotConfig = _legacyGroupSlotConfig;
            }

            if (Save == null && (_legacyAutoSaveInterval != 0 || !string.IsNullOrEmpty(_legacyEncryptionKey)))
            {
                Save = CreateInstance<SaveSettings>();
                if (_legacyAutoSaveInterval != 0) Save.AutoSaveInterval = _legacyAutoSaveInterval;
                if (!string.IsNullOrEmpty(_legacyEncryptionKey)) Save.EncryptionKey = _legacyEncryptionKey;
            }

            if (Log == null && _legacyLogLevel != 0)
            {
                Log = CreateInstance<LogSettings>();
                Log.LogLevel = (LogLevel)_legacyLogLevel;
            }

            if (Config == null && !string.IsNullOrEmpty(_legacyConfigAddressPrefix))
            {
                Config = CreateInstance<ConfigSettings>();
                if (!string.IsNullOrEmpty(_legacyConfigAddressPrefix)) Config.ConfigAddressPrefix = _legacyConfigAddressPrefix;
            }
        }

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