using UnityEditor;

namespace CFramework.Editor.Configs
{
    /// <summary>
    ///     Luban 生成器配置
    ///     <para>基于 EditorPrefs 存储，每个开发者本地独立，不会提交到版本控制</para>
    /// </summary>
    public static class LubanConfig
    {
        private const string KeyPrefix = "CFramework.Luban.";

        // 默认值
        private const string DefaultLubanDllPath = "Tools/Luban/Luban.Core.exe";
        private const string DefaultConfPath = "LubanConfig/luban.conf";
        private const string DefaultTargetName = "client";
        private const string DefaultCodeTarget = "cs-bin";
        private const string DefaultDataTarget = "bin";
        private const string DefaultOutputCodeDir = "Assets/Scripts/Generated/Config";
        private const string DefaultOutputDataDir = "Assets/AddressableAssets/Config";
        private const string DefaultTopModule = "cfg";
        private const bool DefaultCleanOutputDir = true;

        /// <summary>Luban 可执行文件路径（.exe 自包含版 或 .dll 框架依赖版，支持绝对路径或项目相对路径）</summary>
        public static string LubanDllPath
        {
            get => EditorPrefs.GetString(KeyPrefix + "LubanDllPath", DefaultLubanDllPath);
            set => EditorPrefs.SetString(KeyPrefix + "LubanDllPath", value);
        }

        /// <summary>luban.conf 配置文件路径（绝对路径或项目相对路径）</summary>
        public static string ConfPath
        {
            get => EditorPrefs.GetString(KeyPrefix + "ConfPath", DefaultConfPath);
            set => EditorPrefs.SetString(KeyPrefix + "ConfPath", value);
        }

        /// <summary>Target 名称（对应 luban.conf 中 targets 的 name）</summary>
        public static string TargetName
        {
            get => EditorPrefs.GetString(KeyPrefix + "TargetName", DefaultTargetName);
            set => EditorPrefs.SetString(KeyPrefix + "TargetName", value);
        }

        /// <summary>代码生成目标类型（如 cs-bin、cs-simple-json）</summary>
        public static string CodeTarget
        {
            get => EditorPrefs.GetString(KeyPrefix + "CodeTarget", DefaultCodeTarget);
            set => EditorPrefs.SetString(KeyPrefix + "CodeTarget", value);
        }

        /// <summary>数据生成目标类型（如 bin、json）</summary>
        public static string DataTarget
        {
            get => EditorPrefs.GetString(KeyPrefix + "DataTarget", DefaultDataTarget);
            set => EditorPrefs.SetString(KeyPrefix + "DataTarget", value);
        }

        /// <summary>生成的 C# 代码输出目录（绝对路径或项目相对路径）</summary>
        public static string OutputCodeDir
        {
            get => EditorPrefs.GetString(KeyPrefix + "OutputCodeDir", DefaultOutputCodeDir);
            set => EditorPrefs.SetString(KeyPrefix + "OutputCodeDir", value);
        }

        /// <summary>生成的数据文件输出目录（绝对路径或项目相对路径）</summary>
        public static string OutputDataDir
        {
            get => EditorPrefs.GetString(KeyPrefix + "OutputDataDir", DefaultOutputDataDir);
            set => EditorPrefs.SetString(KeyPrefix + "OutputDataDir", value);
        }

        /// <summary>Luban 顶层模块名（对应 luban.conf 中的 topModule）</summary>
        public static string TopModule
        {
            get => EditorPrefs.GetString(KeyPrefix + "TopModule", DefaultTopModule);
            set => EditorPrefs.SetString(KeyPrefix + "TopModule", value);
        }

        /// <summary>生成前清理输出目录</summary>
        public static bool CleanOutputDir
        {
            get => EditorPrefs.GetBool(KeyPrefix + "CleanOutputDir", DefaultCleanOutputDir);
            set => EditorPrefs.SetBool(KeyPrefix + "CleanOutputDir", value);
        }

        /// <summary>数据标签过滤（留空表示不过滤）</summary>
        public static string IncludeTag
        {
            get => EditorPrefs.GetString(KeyPrefix + "IncludeTag", "");
            set => EditorPrefs.SetString(KeyPrefix + "IncludeTag", value);
        }

        /// <summary>排除数据标签（留空表示不排除）</summary>
        public static string ExcludeTag
        {
            get => EditorPrefs.GetString(KeyPrefix + "ExcludeTag", "");
            set => EditorPrefs.SetString(KeyPrefix + "ExcludeTag", value);
        }

        /// <summary>校验失败视为错误</summary>
        public static bool ValidationFailAsError
        {
            get => EditorPrefs.GetBool(KeyPrefix + "ValidationFailAsError", false);
            set => EditorPrefs.SetBool(KeyPrefix + "ValidationFailAsError", value);
        }

        /// <summary>详细日志输出</summary>
        public static bool Verbose
        {
            get => EditorPrefs.GetBool(KeyPrefix + "Verbose", false);
            set => EditorPrefs.SetBool(KeyPrefix + "Verbose", value);
        }

        /// <summary>监视目录变化自动重新生成（开发模式）</summary>
        public static string WatchDir
        {
            get => EditorPrefs.GetString(KeyPrefix + "WatchDir", "");
            set => EditorPrefs.SetString(KeyPrefix + "WatchDir", value);
        }

        /// <summary>
        ///     重置所有配置为默认值
        /// </summary>
        public static void ResetToDefaults()
        {
            LubanDllPath = DefaultLubanDllPath;
            ConfPath = DefaultConfPath;
            TargetName = DefaultTargetName;
            CodeTarget = DefaultCodeTarget;
            DataTarget = DefaultDataTarget;
            OutputCodeDir = DefaultOutputCodeDir;
            OutputDataDir = DefaultOutputDataDir;
            TopModule = DefaultTopModule;
            CleanOutputDir = DefaultCleanOutputDir;
            IncludeTag = "";
            ExcludeTag = "";
            ValidationFailAsError = false;
            Verbose = false;
            WatchDir = "";
        }
    }
}
