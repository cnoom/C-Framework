using CFramework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace CFramework.Editor.Utilities
{
    /// <summary>
    ///     配置资产创建器 - 在编译完成后自动创建资产
    /// </summary>
    [InitializeOnLoad]
    public static class ConfigAssetCreator
    {
        private const string PREF_KEY = "CFramework.PendingConfigAssets";

        static ConfigAssetCreator()
        {
            // 监听编辑器加载完成事件
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            // 检查是否有待处理的资产
            var pendingAssets = LoadPendingAssets();
            if (pendingAssets.assets.Count > 0)
            {
                Debug.Log($"[ConfigAssetCreator] 检测到 {pendingAssets.assets.Count} 个待创建资产");
                // 延迟一帧确保所有脚本加载完成
                EditorApplication.delayCall += ProcessPendingAssets;
            }
        }

        public static void RegisterPendingAsset(string configName, string configNamespace, string outputPath)
        {
            var pendingAssets = LoadPendingAssets();

            pendingAssets.assets.Add(new PendingAssetInfo
            {
                configName = configName,
                configNamespace = configNamespace,
                outputPath = outputPath
            });

            SavePendingAssets(pendingAssets);

            Debug.Log($"[ConfigAssetCreator] 已注册待创建资产：{configName}，当前队列：{pendingAssets.assets.Count} 个");
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            // 编译完成后处理待创建的资产
            var pendingAssets = LoadPendingAssets();
            if (pendingAssets.assets.Count > 0)
            {
                Debug.Log($"[ConfigAssetCreator] 编译完成，准备创建 {pendingAssets.assets.Count} 个资产");
                // 延迟执行，确保所有程序集已加载
                EditorApplication.delayCall += ProcessPendingAssets;
            }
        }

        private static void ProcessPendingAssets()
        {
            var pendingAssets = LoadPendingAssets();

            if (pendingAssets.assets.Count == 0) return;

            Debug.Log($"[ConfigAssetCreator] 开始处理 {pendingAssets.assets.Count} 个待创建资产");

            // 创建待处理资产的副本
            var assetsToCreate = pendingAssets.assets.ToArray();

            // 清空待创建列表
            pendingAssets.assets.Clear();
            SavePendingAssets(pendingAssets);

            foreach (var assetInfo in assetsToCreate) CreateConfigAsset(assetInfo);
        }

        private static PendingAssetList LoadPendingAssets()
        {
            var json = EditorPrefs.GetString(PREF_KEY, "");
            if (string.IsNullOrEmpty(json)) return new PendingAssetList();

            try
            {
                return JsonUtility.FromJson<PendingAssetList>(json);
            }
            catch
            {
                return new PendingAssetList();
            }
        }

        private static void SavePendingAssets(PendingAssetList pendingAssets)
        {
            var json = JsonUtility.ToJson(pendingAssets);
            EditorPrefs.SetString(PREF_KEY, json);
        }

        private static void CreateConfigAsset(PendingAssetInfo assetInfo)
        {
            var configType = FindConfigType(assetInfo.configName, assetInfo.configNamespace);

            if (configType == null)
            {
                Debug.LogWarning($"[ConfigCreator] 无法找到配置类型：{assetInfo.configName}");
                Debug.LogWarning("[ConfigCreator] 可能原因：命名空间配置错误或脚本存在编译错误");
                Debug.LogWarning($"[ConfigCreator] 请手动创建资产：Assets/Create/Game/Config/{assetInfo.configName}");

                EditorUtility.DisplayDialog("资产创建失败",
                    $"无法找到配置类型：{assetInfo.configName}\n\n" +
                    "可能原因：\n" +
                    "1. 命名空间配置错误\n" +
                    "2. 脚本存在编译错误\n\n" +
                    "请手动创建资产：\n" +
                    $"Assets/Create/Game/Config/{assetInfo.configName}",
                    "确定");
                return;
            }

            try
            {
                // 确保目录存在
                if (!Directory.Exists(assetInfo.outputPath)) Directory.CreateDirectory(assetInfo.outputPath);

                // 创建配置资产
                var asset = ScriptableObject.CreateInstance(configType);
                var assetPath = Path.Combine(assetInfo.outputPath, $"{assetInfo.configName}.asset");

                // 检查资产是否已存在
                var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (existingAsset != null)
                {
                    Debug.LogWarning($"[ConfigCreator] 资产已存在，跳过创建：{assetPath}");
                    Selection.activeObject = existingAsset;
                    EditorGUIUtility.PingObject(existingAsset);
                    return;
                }

                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[ConfigCreator] 创建配置资产成功：{assetPath}");

                EditorUtility.DisplayDialog("资产创建成功",
                    "配置资产创建成功！\n\n" +
                    $"路径：{assetPath}",
                    "确定");

                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigCreator] 创建资产失败：{ex.Message}");
                EditorUtility.DisplayDialog("资产创建失败",
                    $"创建资产时发生错误：{ex.Message}\n\n" +
                    "请手动创建资产：\n" +
                    $"Assets/Create/Game/Config/{assetInfo.configName}",
                    "确定");
            }
        }

        private static Type FindConfigType(string configName, string configNamespace)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // 方式1：完整命名空间（优先）
            foreach (var assembly in assemblies)
                try
                {
                    var fullTypeName = !string.IsNullOrEmpty(configNamespace)
                        ? $"{configNamespace}.{configName}"
                        : configName;

                    var type = assembly.GetType(fullTypeName);
                    if (type != null && typeof(ConfigTableAsset).IsAssignableFrom(type)) return type;
                }
                catch (Exception)
                {
                    // 忽略异常，继续查找
                }

            // 方式2：不带命名空间
            foreach (var assembly in assemblies)
                try
                {
                    var type = assembly.GetType(configName);
                    if (type != null && typeof(ConfigTableAsset).IsAssignableFrom(type)) return type;
                }
                catch (Exception)
                {
                    // 忽略异常，继续查找
                }

            return null;
        }

        [Serializable]
        private class PendingAssetList
        {
            public List<PendingAssetInfo> assets = new();
        }

        [Serializable]
        private class PendingAssetInfo
        {
            public string configName;
            public string configNamespace;
            public string outputPath;
        }
    }
}
