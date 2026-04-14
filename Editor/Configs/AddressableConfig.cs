using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CFramework.Editor.Generators;
using CFramework.Editor.Windows.Addressable;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace CFramework.Editor.Configs
{
    /// <summary>
    ///     Addressables 全局配置
    /// </summary>
    [CreateAssetMenu(fileName = "AddressableConfig", menuName = "CFramework/Addressables/全局配置")]
    public sealed class AddressableConfig : ScriptableObject
    {
        #region 目录配置列表

        [PropertyOrder(-10)]
        [TitleGroup("目录配置列表", Subtitle = "配置需要自动设置 Addressables 的目录")]
        [ListDrawerSettings(
            ShowPaging = true,
            NumberOfItemsPerPage = 10,
            DraggableItems = true,
            ShowItemCount = true,
            CustomAddFunction = nameof(CreateNewDirectoryConfig)
        )]
        [OnCollectionChanged(nameof(OnConfigListChanged))]
        public List<DirectoryConfig> directories = new();

        #endregion

        #region 统计信息

        [PropertyOrder(100)]
        [FoldoutGroup("统计信息")]
        [ShowInInspector]
        [DisplayAsString(Sirenix.OdinInspector.TextAlignment.Left, Overflow = false)]
        [HideLabel]
        private string Statistics
        {
            get
            {
                var enabled = directories.Count(c => c.enabled);
                var disabled = directories.Count(c => !c.enabled);
                var simulation = directories.Count(c => c.simulationMode);
                var totalAssets = directories.Where(c => c.enabled).Sum(c => GetMatchingAssets(c).Count);

                return $"总配置数: {directories.Count}\n" +
                       $"已启用: {enabled}\n" +
                       $"已禁用: {disabled}\n" +
                       $"模拟模式: {simulation}\n" +
                       $"预计处理资源: {totalAssets}";
            }
        }

        #endregion

        #region 快捷操作

        [PropertyOrder(-5)]
        [FoldoutGroup("快捷操作")]
        [Button("预览所有配置", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void PreviewAllConfigs()
        {
            var enabledConfigs = directories.Where(c => c.enabled).ToList();

            if (enabledConfigs.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有启用的目录配置", "确定");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== 批量配置预览 ===\n");

            foreach (var config in enabledConfigs)
            {
                sb.AppendLine(PreviewDirectoryConfig(config));
                sb.AppendLine("\n" + new string('=', 50) + "\n");
            }

            AddressableConfigPreviewWindow.ShowWindow(sb.ToString());
        }

        [PropertyOrder(-4)]
        [FoldoutGroup("快捷操作")]
        [Button("应用所有配置", ButtonSizes.Large)]
        [GUIColor(0.8f, 1f, 0.4f)]
        private void ApplyAllConfigs()
        {
            var enabledConfigs = directories.Where(c => c.enabled && !c.simulationMode).ToList();

            if (enabledConfigs.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有启用且非模拟模式的配置", "确定");
                return;
            }

            var totalAssets = enabledConfigs.Sum(c => GetMatchingAssets(c).Count);

            if (!EditorUtility.DisplayDialog("确认应用",
                    $"确定要应用 {enabledConfigs.Count} 个目录配置吗？\n\n将处理约 {totalAssets} 个资源\n此操作将修改资源的 Addressable 设置。",
                    "确定", "取消"))
                return;

            var processedCount = 0;
            foreach (var config in enabledConfigs)
            {
                ApplyDirectoryConfig(config);
                processedCount++;
                EditorUtility.DisplayProgressBar("应用配置",
                    $"正在处理: {config.configName}",
                    (float)processedCount / enabledConfigs.Count);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("完成", $"配置应用完成！\n共处理 {enabledConfigs.Count} 个目录配置", "确定");
        }

        [PropertyOrder(-3)]
        [FoldoutGroup("快捷操作")]
        [Button("刷新配置", ButtonSizes.Medium)]
        private void RefreshConfig()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log("[AddressableConfig] 配置已刷新");
        }

        #endregion

        #region 常量生成设置

        [PropertyOrder(50)]
        [FoldoutGroup("常量生成设置")]
        [LabelText("生成输出路径")]
        [FolderPath(RequireExistingPath = true)]
        [Tooltip("常量文件的输出目录")]
        public string constantsOutputPath = EditorPaths.AddressableConstantsOutput;

        [PropertyOrder(51)] [FoldoutGroup("常量生成设置")] [LabelText("生成的类名")] [Tooltip("生成的常量类名称")]
        public string constantsClassName = "Address";

        [PropertyOrder(52)] [FoldoutGroup("常量生成设置")] [LabelText("命名空间")] [Tooltip("常量类的命名空间，留空则不使用命名空间")]
        public string constantsNamespace = "CFramework";

        [PropertyOrder(53)] [FoldoutGroup("常量生成设置")] [LabelText("自动生成")] [Tooltip("当 Addressables 变更时自动重新生成常量文件")]
        public bool autoGenerate = true;

        [PropertyOrder(54)] [FoldoutGroup("常量生成设置")] [LabelText("启用嵌套分组")] [Tooltip("按地址路径的第一级目录创建嵌套类")]
        public bool enableNestedGroups = true;

        [PropertyOrder(55)] [FoldoutGroup("常量生成设置")] [LabelText("命名转换规则")] [Tooltip("常量名的命名风格")]
        public NamingConvention namingConvention = NamingConvention.PascalCase;

        [PropertyOrder(56)]
        [FoldoutGroup("常量生成设置")]
        [LabelText("排除的分组")]
        [Tooltip("不参与常量生成的分组名称")]
        [ListDrawerSettings(ShowPaging = false)]
        public List<string> excludedGroups = new() { "Built In Data" };

        [PropertyOrder(57)]
        [FoldoutGroup("常量生成设置")]
        [Button("生成资源常量", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.6f, 1f)]
        private void GenerateConstantsButton()
        {
            AddressableConstantsGenerator.Generate(this);
        }

        [PropertyOrder(58)]
        [FoldoutGroup("常量生成设置")]
        [Button("打开输出目录", ButtonSizes.Medium)]
        private void OpenOutputDirectory()
        {
            var path = constantsOutputPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }

            EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }

        #endregion

        #region 目录配置类

        /// <summary>
        ///     目录配置
        /// </summary>
        [Serializable]
        [HideLabel]
        public sealed class DirectoryConfig
        {
            #region 基础配置

            [PropertyOrder(-100)] [LabelText("配置名称")] [Required]
            public string configName = "新配置";

            [PropertyOrder(-99)]
            [FolderPath(RequireExistingPath = true)]
            [LabelText("目标目录")]
            [OnValueChanged(nameof(OnTargetDirectoryChanged))]
            public string targetDirectory = "Assets";

            [PropertyOrder(-98)] [LabelText("启用")] public bool enabled = true;

            #endregion

            #region 可寻址命名规则

            [FoldoutGroup("可寻址命名规则")]
            [LabelText("前缀类型")]
            [Tooltip("地址前缀的类型")]
            [ValueDropdown(nameof(PrefixTypeOptions))]
            public PrefixType prefixType = PrefixType.GroupName;

            private IEnumerable<PrefixType> PrefixTypeOptions = new[]
            {
                PrefixType.None,
                PrefixType.DirectoryName,
                PrefixType.GroupName,
                PrefixType.LabelName,
                PrefixType.Custom
            };

            [FoldoutGroup("可寻址命名规则")]
            [LabelText("自定义前缀")]
            [ShowIf("@prefixType == PrefixType.Custom")]
            [Tooltip("自定义前缀文本")]
            public string customPrefix = "";

            [FoldoutGroup("可寻址命名规则")]
            [LabelText("地址类型")]
            [Tooltip("地址部分的格式")]
            [ValueDropdown(nameof(AddressTypeOptions))]
            public AddressType addressType = AddressType.FileName;

            private IEnumerable<AddressType> AddressTypeOptions = new[]
            {
                AddressType.FileName,
                AddressType.FileNameWithExtension,
                AddressType.RelativePath
            };

            [FoldoutGroup("可寻址命名规则")] [LabelText("转为小写")]
            public bool convertToLowercase = true;

            [FoldoutGroup("可寻址命名规则")] [LabelText("替换空格为下划线")]
            public bool replaceSpacesWithUnderscore = true;

            #endregion

            #region 分组设置

            [FoldoutGroup("分组设置")] [LabelText("分组名称")] [Required]
            public string groupName = "Default";

            [FoldoutGroup("分组设置")] [LabelText("使用子目录作为分组")] [Tooltip("如果启用，每个子目录将创建为独立分组")]
            public bool useSubDirectoryAsGroup;

            [FoldoutGroup("分组设置")] [LabelText("分组命名前缀")] [ShowIf(nameof(useSubDirectoryAsGroup))]
            public string groupPrefix = "";

            #endregion

            #region 标签规则

            [FoldoutGroup("标签规则")] [LabelText("默认标签")] [ValueDropdown(nameof(GetAllLabels), IsUniqueList = true)]
            public List<string> defaultLabels = new();

            [FoldoutGroup("标签规则")] [LabelText("标签命名规则")] [ListDrawerSettings(ShowPaging = false)]
            public List<LabelRule> labelRules = new();

            #endregion

            #region 过滤规则

            [FoldoutGroup("过滤规则")] [LabelText("包含的文件类型")] [Tooltip("留空表示包含所有类型")]
            public List<string> includeExtensions = new() { ".prefab", ".asset", ".mat", ".png", ".wav", ".mp3" };

            [FoldoutGroup("过滤规则")] [LabelText("排除的文件")] [ListDrawerSettings(ShowPaging = false)]
            public List<string> excludeFiles = new();

            [FoldoutGroup("过滤规则")] [LabelText("排除的目录")] [ListDrawerSettings(ShowPaging = false)]
            public List<string> excludeDirectories = new();

            [FoldoutGroup("过滤规则")] [LabelText("递归处理子目录")]
            public bool recursive = true;

            [FoldoutGroup("过滤规则")] [LabelText("忽略以点开头的目录")] [Tooltip("例如 .git, .vs 等")]
            public bool ignoreDotDirectories = true;

            #endregion

            #region 高级设置

            [FoldoutGroup("高级设置")] [LabelText("模拟模式")] [Tooltip("启用后只会预览不会实际修改资源")]
            public bool simulationMode = true;

            [FoldoutGroup("高级设置")] [LabelText("显示详细日志")]
            public bool verboseLogging;

            #endregion

            #region 操作按钮

            [HorizontalGroup("操作", 0.5f)]
            [Button("预览", ButtonSizes.Medium)]
            [GUIColor(0.4f, 0.8f, 1f)]
            [PropertyOrder(1000)]
            private void PreviewButton()
            {
                var preview = PreviewDirectoryConfig(this);
                AddressableConfigPreviewWindow.ShowWindow(preview);
            }

            [HorizontalGroup("操作", 0.5f)]
            [Button("应用", ButtonSizes.Medium)]
            [GUIColor(0.8f, 1f, 0.4f)]
            [EnableIf(nameof(enabled))]
            [PropertyOrder(1001)]
            private void ApplyButton()
            {
                if (simulationMode)
                {
                    EditorUtility.DisplayDialog("模拟模式", "当前处于模拟模式，不会实际修改资源。\n请取消勾选'模拟模式'后再应用。", "确定");
                    return;
                }

                if (!EditorUtility.DisplayDialog("确认应用",
                        $"确定要将配置 '{configName}' 应用到目录 '{targetDirectory}' 吗？\n\n此操作将修改资源的 Addressable 设置。",
                        "确定", "取消"))
                    return;

                ApplyDirectoryConfig(this);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("完成", "配置应用成功！", "确定");
            }

            #endregion

            #region 辅助方法

            private void OnTargetDirectoryChanged()
            {
                if (string.IsNullOrEmpty(configName) || configName == "新配置")
                    configName = Path.GetFileName(targetDirectory);
            }

            private IEnumerable<string> GetAllLabels()
            {
                return AddressableAssetSettingsDefaultObject.Settings?.GetLabels() ?? new List<string>();
            }

            #endregion
        }

        /// <summary>
        ///     前缀类型
        /// </summary>
        public enum PrefixType
        {
            None, // 无前缀
            DirectoryName, // 目录名
            GroupName, // 组名
            LabelName, // 标签名（第一个标签）
            Custom // 自定义
        }

        /// <summary>
        ///     地址类型
        /// </summary>
        public enum AddressType
        {
            FileName, // 文件名（无后缀）
            FileNameWithExtension, // 文件名+后缀
            RelativePath // 相对路径
        }

        /// <summary>
        ///     标签规则
        /// </summary>
        [Serializable]
        public sealed class LabelRule
        {
            [LabelText("匹配模式")] [Tooltip("支持通配符 * 和 ?")]
            public string pattern = "*";

            [LabelText("添加标签")] [ValueDropdown(nameof(GetAllLabels), IsUniqueList = true)]
            public List<string> labels = new();

            private IEnumerable<string> GetAllLabels()
            {
                return AddressableAssetSettingsDefaultObject.Settings?.GetLabels() ?? new List<string>();
            }
        }

        #endregion

        #region 处理方法

        private DirectoryConfig CreateNewDirectoryConfig()
        {
            return new DirectoryConfig();
        }

        private void OnConfigListChanged()
        {
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        ///     预览目录配置
        /// </summary>
        private static string PreviewDirectoryConfig(DirectoryConfig config)
        {
            if (config == null) return "配置为空";

            var sb = new StringBuilder();
            sb.AppendLine($"=== 配置预览: {config.configName} ===\n");
            sb.AppendLine($"目标目录: {config.targetDirectory}");
            sb.AppendLine($"启用状态: {(config.enabled ? "已启用" : "已禁用")}");
            sb.AppendLine($"模拟模式: {(config.simulationMode ? "开启" : "关闭")}\n");

            // 获取匹配的资源
            var assets = GetMatchingAssets(config);
            sb.AppendLine($"匹配的资源数量: {assets.Count}\n");

            if (assets.Count == 0)
            {
                sb.AppendLine("未找到匹配的资源");
                return sb.ToString();
            }

            // 显示前10个资源的预览
            sb.AppendLine("--- 资源预览 (最多显示前10个) ---\n");
            var previewCount = Mathf.Min(10, assets.Count);

            for (var i = 0; i < previewCount; i++)
            {
                var assetPath = assets[i];
                var address = GenerateAddress(config, assetPath);
                var group = DetermineGroup(config, assetPath);
                var labels = DetermineLabels(config, assetPath);

                sb.AppendLine($"[{i + 1}] {Path.GetFileName(assetPath)}");
                sb.AppendLine($"    路径: {assetPath}");
                sb.AppendLine($"    地址: {address}");
                sb.AppendLine($"    分组: {group}");
                sb.AppendLine($"    标签: {string.Join(", ", labels)}");
                sb.AppendLine();
            }

            if (assets.Count > previewCount) sb.AppendLine($"... 还有 {assets.Count - previewCount} 个资源未显示\n");

            // 统计信息
            sb.AppendLine("--- 统计信息 ---\n");
            var groups = assets.Select(a => DetermineGroup(config, a)).Distinct().ToList();
            sb.AppendLine($"将创建/使用的分组: {string.Join(", ", groups)}");

            var allLabels = assets.SelectMany(a => DetermineLabels(config, a)).Distinct().ToList();
            sb.AppendLine($"涉及的标签: {string.Join(", ", allLabels)}");

            return sb.ToString();
        }

        /// <summary>
        ///     应用目录配置
        /// </summary>
        private static void ApplyDirectoryConfig(DirectoryConfig config)
        {
            if (config == null || !config.enabled) return;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AddressableConfig] Addressable Asset Settings not found!");
                return;
            }

            var assets = GetMatchingAssets(config);
            var processedCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var assetPath in assets)
                {
                    // 获取或创建分组
                    var groupName = DetermineGroup(config, assetPath);
                    var group = settings.FindGroup(groupName);
                    if (group == null) group = settings.CreateGroup(groupName, false, false, true, null);

                    // 获取或创建 Addressable 条目
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    var entry = settings.CreateOrMoveEntry(guid, group, false, false);

                    if (entry != null)
                    {
                        // 设置地址
                        entry.address = GenerateAddress(config, assetPath);

                        // 设置标签
                        var labels = DetermineLabels(config, assetPath);
                        foreach (var label in labels) entry.labels.Add(label);

                        processedCount++;

                        if (config.verboseLogging)
                            Debug.Log(
                                $"[AddressableConfig] Processed: {assetPath} -> {entry.address} in group {groupName}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[AddressableConfig] 配置 '{config.configName}' 应用完成，共处理 {processedCount} 个资源");
        }

        /// <summary>
        ///     获取匹配的资源
        /// </summary>
        internal static List<string> GetMatchingAssets(DirectoryConfig config)
        {
            var result = new List<string>();

            if (string.IsNullOrEmpty(config.targetDirectory) || !Directory.Exists(config.targetDirectory))
                return result;

            var searchOption = config.recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(config.targetDirectory, "*.*", searchOption);

            foreach (var file in files)
                if (ShouldIncludeFile(file, config))
                    result.Add(file.Replace("\\", "/"));

            return result;
        }

        /// <summary>
        ///     判断文件是否应该被包含
        /// </summary>
        private static bool ShouldIncludeFile(string filePath, DirectoryConfig config)
        {
            // 忽略 meta 文件
            if (filePath.EndsWith(".meta")) return false;

            // 检查文件扩展名
            var extension = Path.GetExtension(filePath).ToLower();
            if (config.includeExtensions != null && config.includeExtensions.Count > 0)
                if (!config.includeExtensions.Contains(extension))
                    return false;

            // 检查排除的文件
            var fileName = Path.GetFileName(filePath);
            if (config.excludeFiles != null && config.excludeFiles.Contains(fileName)) return false;

            // 检查排除的目录
            var directory = Path.GetDirectoryName(filePath)?.Replace("\\", "/");
            if (config.excludeDirectories != null)
                foreach (var excludeDir in config.excludeDirectories)
                    if (directory != null && directory.StartsWith(excludeDir, StringComparison.OrdinalIgnoreCase))
                        return false;

            // 检查以点开头的目录
            if (config.ignoreDotDirectories)
            {
                var relativePath = filePath.Substring(config.targetDirectory.Length).TrimStart('/');
                var pathParts = relativePath.Split('/');
                if (pathParts.Any(part => part.StartsWith("."))) return false;
            }

            return true;
        }

        /// <summary>
        ///     生成可寻址地址
        /// </summary>
        internal static string GenerateAddress(DirectoryConfig config, string assetPath)
        {
            var relativePath = assetPath.Substring(config.targetDirectory.Length).TrimStart('/');
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var fileNameWithExt = Path.GetFileName(assetPath);
            var directoryName = Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? "";

            // 生成前缀部分
            var prefix = config.prefixType switch
            {
                PrefixType.None => "",
                PrefixType.DirectoryName => Path.GetFileName(config.targetDirectory),
                PrefixType.GroupName => config.groupName,
                PrefixType.LabelName => config.defaultLabels.Count > 0 ? config.defaultLabels[0] : "",
                PrefixType.Custom => config.customPrefix,
                _ => ""
            };

            // 生成地址部分
            var address = config.addressType switch
            {
                AddressType.FileName => fileName,
                AddressType.FileNameWithExtension => fileNameWithExt,
                AddressType.RelativePath => Path.Combine(Path.GetDirectoryName(relativePath) ?? "", fileName),
                _ => fileName
            };

            // 组合成最终地址：前缀/地址
            var finalAddress = string.IsNullOrEmpty(prefix)
                ? address
                : $"{prefix}/{address}";

            // 格式化处理
            if (config.convertToLowercase) finalAddress = finalAddress.ToLower();

            if (config.replaceSpacesWithUnderscore) finalAddress = finalAddress.Replace(" ", "_");

            return finalAddress;
        }

        /// <summary>
        ///     确定分组名称
        /// </summary>
        internal static string DetermineGroup(DirectoryConfig config, string assetPath)
        {
            if (!config.useSubDirectoryAsGroup) return config.groupName;

            var relativePath = assetPath.Substring(config.targetDirectory.Length).TrimStart('/');
            var pathParts = relativePath.Split('/');

            if (pathParts.Length > 1)
            {
                var subDirectory = pathParts[0];
                return config.groupPrefix + subDirectory;
            }

            return config.groupName;
        }

        /// <summary>
        ///     确定标签列表
        /// </summary>
        internal static List<string> DetermineLabels(DirectoryConfig config, string assetPath)
        {
            var labels = new List<string>(config.defaultLabels);

            // 应用标签规则
            var fileName = Path.GetFileName(assetPath);
            foreach (var rule in config.labelRules)
                if (IsMatch(fileName, rule.pattern))
                    labels.AddRange(rule.labels);

            return labels.Distinct().ToList();
        }

        /// <summary>
        ///     简单的通配符匹配
        /// </summary>
        private static bool IsMatch(string input, string pattern)
        {
            // 简化实现，仅支持 * 通配符
            if (string.IsNullOrEmpty(pattern) || pattern == "*") return true;

            if (pattern.StartsWith("*") && pattern.EndsWith("*"))
            {
                var middle = pattern.Substring(1, pattern.Length - 2);
                return input.Contains(middle);
            }

            if (pattern.StartsWith("*"))
            {
                var suffix = pattern.Substring(1);
                return input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            if (pattern.EndsWith("*"))
            {
                var prefix = pattern.Substring(0, pattern.Length - 1);
                return input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return input.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 静态方法

        private static AddressableConfig _instance;

        /// <summary>
        ///     获取或创建默认实例
        /// </summary>
        /// <param name="createIfNotFound">如果找不到是否创建新实例</param>
        public static AddressableConfig GetOrCreateInstance(bool createIfNotFound = true)
        {
            if (_instance != null) return _instance;

            // 查找现有实例
            var guids = AssetDatabase.FindAssets("t:AddressableConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _instance = AssetDatabase.LoadAssetAtPath<AddressableConfig>(path);
                return _instance;
            }

            if (!createIfNotFound) return null;

            // 创建新实例
            var defaultPath = EditorPaths.AddressableConfig;

            // 确保目录存在
            EditorPaths.EnsureDirectoryExists(EditorPaths.EditorConfigs);

            _instance = CreateInstance<AddressableConfig>();
            AssetDatabase.CreateAsset(_instance, defaultPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AddressableConfig] 创建默认配置: {defaultPath}");

            return _instance;
        }

        #endregion
    }
}