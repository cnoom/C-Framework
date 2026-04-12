using System.Collections.Generic;
using System.Linq;
using CFramework.Editor.Configs;
using CFramework.Editor.Generators;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace CFramework.Editor.Utilities
{
    /// <summary>
    ///     Addressables 资源变更监听器
    ///     当资源变更时：先更新受影响目录的 Addressable 地址，再重新生成常量文件
    /// </summary>
    public sealed class AddressableAssetPostprocessor : AssetPostprocessor
    {
        /// <summary>
        ///     Addressables 配置目录
        /// </summary>
        private const string AddressablesDataPath = "Assets/AddressableAssetsData";

        /// <summary>
        ///     暂存的变更资源列表，用于延迟处理
        /// </summary>
        private static readonly List<AssetChangeInfo> _pendingChanges = new();

        /// <summary>
        ///     资源后处理回调
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // 检查是否需要处理
            if (!ShouldRegenerate(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths))
                return;

            // 暂存变更信息
            lock (_pendingChanges)
            {
                _pendingChanges.Add(new AssetChangeInfo
                {
                    ImportedAssets = importedAssets,
                    DeletedAssets = deletedAssets,
                    MovedAssets = movedAssets,
                    MovedFromAssetPaths = movedFromAssetPaths
                });
            }

            // 延迟执行，避免频繁触发（多次回调会合并处理）
            EditorApplication.delayCall -= TriggerGeneration;
            EditorApplication.delayCall += TriggerGeneration;
        }

        /// <summary>
        ///     判断是否需要重新生成常量文件
        ///     仅在标记的目录下资源发生变更时才触发
        /// </summary>
        private static bool ShouldRegenerate(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // 仅检查受监控目录内资源的变更
            return HasWatchedDirectoryChange(importedAssets) ||
                   HasWatchedDirectoryChange(deletedAssets) ||
                   HasWatchedDirectoryChange(movedAssets) ||
                   HasWatchedDirectoryChange(movedFromAssetPaths);
        }

        /// <summary>
        ///     检查是否有受监控目录内的资源变更
        /// </summary>
        private static bool HasWatchedDirectoryChange(string[] assets)
        {
            if (assets == null)
                return false;

            var config = AddressableConfig.GetOrCreateInstance(false);
            if (config == null || config.directories == null)
                return false;

            foreach (var assetPath in assets)
            {
                // 跳过 meta 文件
                if (assetPath.EndsWith(".meta"))
                    continue;

                // 跳过非资源文件类型（脚本、项目文件、临时文件等）
                if (IsNonAssetFile(assetPath))
                    continue;

                // 跳过 Addressables 配置目录
                if (assetPath.StartsWith(AddressablesDataPath))
                    continue;

                // 跳过 CFramework 目录
                if (assetPath.Contains("CFramework/"))
                    continue;

                // 检查是否在某个已启用的目录配置范围内
                foreach (var dirConfig in config.directories)
                {
                    if (!dirConfig.enabled)
                        continue;

                    if (assetPath.StartsWith(dirConfig.targetDirectory)) return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     判断是否为非资源文件（不应触发自动生成的文件类型）
        /// </summary>
        private static bool IsNonAssetFile(string path)
        {
            return path.EndsWith(".cs") ||
                   path.EndsWith(".csproj") ||
                   path.EndsWith(".sln") ||
                   path.EndsWith(".wlt") ||
                   path.EndsWith(".tmp");
        }

        /// <summary>
        ///     触发常量生成
        /// </summary>
        private static void TriggerGeneration()
        {
            // 检查是否处于编译或播放状态
            if (EditorApplication.isCompiling || EditorApplication.isPlaying)
            {
                EditorApplication.update += WaitForCompilation;
                return;
            }

            DoGenerate();
        }

        /// <summary>
        ///     等待编译完成
        /// </summary>
        private static void WaitForCompilation()
        {
            if (EditorApplication.isCompiling)
                return;

            EditorApplication.update -= WaitForCompilation;
            DoGenerate();
        }

        /// <summary>
        ///     执行生成
        ///     先更新受影响目录的资源地址，再生成常量文件
        /// </summary>
        private static void DoGenerate()
        {
            var config = AddressableConfig.GetOrCreateInstance(false);
            if (config == null)
                return;

            // 检查是否启用自动生成
            if (!config.autoGenerate)
                return;

            // 检查是否有有效的 Addressables 设置
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;

            // 收集所有变更的资源路径
            HashSet<string> allChangedAssets;
            HashSet<string> allDeletedAssets;

            lock (_pendingChanges)
            {
                allChangedAssets = new HashSet<string>();
                allDeletedAssets = new HashSet<string>();

                foreach (var change in _pendingChanges)
                {
                    AddRangeSafe(allChangedAssets, change.ImportedAssets);
                    AddRangeSafe(allChangedAssets, change.MovedAssets);
                    AddRangeSafe(allDeletedAssets, change.DeletedAssets);
                    // 移动前的路径也需要作为删除处理
                    AddRangeSafe(allDeletedAssets, change.MovedFromAssetPaths);
                }

                _pendingChanges.Clear();
            }

            // 第一步：处理受影响目录配置的资源地址
            var affectedConfigs = FindAffectedDirectoryConfigs(config, allChangedAssets, allDeletedAssets);
            if (affectedConfigs.Count > 0)
            {
                Debug.Log($"[AddressableAssetPostprocessor] 检测到资源变更，更新 {affectedConfigs.Count} 个受影响的目录配置...");
                ProcessAddressChanges(config, affectedConfigs, allDeletedAssets);
            }

            // 第二步：清理已删除资源的 Addressable 条目
            CleanupDeletedEntries(config, settings, allDeletedAssets);

            // 第三步：重新生成常量文件
            Debug.Log("[AddressableAssetPostprocessor] 重新生成常量文件...");
            AddressableConstantsGenerator.Generate(config);
        }

        /// <summary>
        ///     安全地添加集合元素
        /// </summary>
        private static void AddRangeSafe(HashSet<string> target, string[] source)
        {
            if (source == null)
                return;

            foreach (var item in source)
                if (!string.IsNullOrEmpty(item) && !item.EndsWith(".meta"))
                    target.Add(item);
        }

        /// <summary>
        ///     找出受影响的所有目录配置
        /// </summary>
        private static List<AddressableConfig.DirectoryConfig> FindAffectedDirectoryConfigs(
            AddressableConfig config,
            HashSet<string> changedAssets,
            HashSet<string> deletedAssets)
        {
            var result = new List<AddressableConfig.DirectoryConfig>();

            if (config.directories == null)
                return result;

            foreach (var dirConfig in config.directories)
            {
                if (!dirConfig.enabled || dirConfig.simulationMode)
                    continue;

                var targetDir = dirConfig.targetDirectory;
                if (string.IsNullOrEmpty(targetDir))
                    continue;

                // 检查变更或删除的资源是否在此目录配置范围内
                bool IsUnderTarget(string path)
                {
                    return path.StartsWith(targetDir);
                }

                if (changedAssets.Any(IsUnderTarget) || deletedAssets.Any(IsUnderTarget)) result.Add(dirConfig);
            }

            return result;
        }

        /// <summary>
        ///     处理受影响目录配置的 Addressable 地址更新
        ///     只处理发生变更的目录配置，而非全部
        /// </summary>
        private static void ProcessAddressChanges(
            AddressableConfig config,
            List<AddressableConfig.DirectoryConfig> affectedConfigs,
            HashSet<string> deletedAssets)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var dirConfig in affectedConfigs)
                {
                    // 重新扫描该目录配置下的所有资源，更新地址
                    var currentAssets = AddressableConfig.GetMatchingAssets(dirConfig);
                    var processedCount = 0;

                    foreach (var assetPath in currentAssets)
                    {
                        // 获取或创建分组
                        var groupName = AddressableConfig.DetermineGroup(dirConfig, assetPath);
                        var group = settings.FindGroup(groupName);
                        if (group == null) group = settings.CreateGroup(groupName, false, false, true, null);

                        // 获取或创建 Addressable 条目
                        var guid = AssetDatabase.AssetPathToGUID(assetPath);
                        var entry = settings.CreateOrMoveEntry(guid, group, false, false);

                        if (entry != null)
                        {
                            // 更新地址
                            entry.address = AddressableConfig.GenerateAddress(dirConfig, assetPath);

                            // 更新标签（先清空旧标签）
                            entry.labels.Clear();
                            var labels = AddressableConfig.DetermineLabels(dirConfig, assetPath);
                            foreach (var label in labels) entry.labels.Add(label);

                            processedCount++;
                        }
                    }

                    Debug.Log(
                        $"[AddressableAssetPostprocessor] 目录配置 '{dirConfig.configName}' 更新完成，处理 {processedCount} 个资源");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        ///     清理已删除资源的 Addressable 条目
        /// </summary>
        private static void CleanupDeletedEntries(
            AddressableConfig config,
            AddressableAssetSettings settings,
            HashSet<string> deletedAssets)
        {
            if (deletedAssets.Count == 0)
                return;

            var removedCount = 0;

            foreach (var deletedPath in deletedAssets)
            {
                // 跳过非受监控目录的删除
                if (!IsUnderAnyWatchedDirectory(config, deletedPath))
                    continue;

                var guid = AssetDatabase.AssetPathToGUID(deletedPath);
                if (string.IsNullOrEmpty(guid))
                    continue;

                var entry = settings.FindAssetEntry(guid);
                if (entry != null)
                {
                    var group = entry.parentGroup;
                    if (group != null)
                    {
                        group.RemoveAssetEntry(entry);
                        removedCount++;

                        if (config.directories != null)
                        {
                            var dirConfig = config.directories.FirstOrDefault(c =>
                                c.enabled && deletedPath.StartsWith(c.targetDirectory));
                            if (dirConfig != null && dirConfig.verboseLogging)
                                Debug.Log($"[AddressableAssetPostprocessor] 移除已删除资源条目: {deletedPath}");
                        }
                    }
                }
            }

            if (removedCount > 0)
            {
                Debug.Log($"[AddressableAssetPostprocessor] 清理 {removedCount} 个已删除资源的 Addressable 条目");
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        ///     判断路径是否在某个受监控的目录下
        /// </summary>
        private static bool IsUnderAnyWatchedDirectory(AddressableConfig config, string assetPath)
        {
            if (config.directories == null)
                return false;

            foreach (var dirConfig in config.directories)
            {
                if (!dirConfig.enabled)
                    continue;

                if (assetPath.StartsWith(dirConfig.targetDirectory)) return true;
            }

            return false;
        }

        /// <summary>
        ///     资源变更信息
        /// </summary>
        private struct AssetChangeInfo
        {
            public string[] ImportedAssets;
            public string[] DeletedAssets;
            public string[] MovedAssets;
            public string[] MovedFromAssetPaths;
        }
    }
}