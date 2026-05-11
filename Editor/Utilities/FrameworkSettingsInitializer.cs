#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor
{
    /// <summary>
    ///     CFramework 一键初始化工具
    ///     <para>自动生成 FrameworkSettings 及所有模块子 Settings 的 .asset 文件</para>
    ///     <para>支持自定义输出路径，自动建立引用关系</para>
    /// </summary>
    public static class FrameworkSettingsInitializer
    {
        private const string MenuPath = "CFramework/初始化框架设置";
        private const string DefaultOutputRoot = EditorPaths.FrameworkSettings;
        private const string EditorPrefsKey = "CFramework_SettingsInit_OutputPath";

        /// <summary>
        ///     子 Settings 元数据描述
        /// </summary>
        private readonly struct SubSettingInfo
        {
            public readonly string TypeName;
            public readonly string FileName;
            public readonly string FieldName;
            public readonly Type Type;

            public SubSettingInfo(string typeName, string fileName, string fieldName, Type type)
            {
                TypeName = typeName;
                FileName = fileName;
                FieldName = fieldName;
                Type = type;
            }
        }

        /// <summary>
        ///     所有子 Settings 定义（与 FrameworkSettings 中的字段一一对应）
        /// </summary>
        private static readonly SubSettingInfo[] SubSettings =
        {
            new("AssetSettings", "AssetSettings.asset", "Asset", typeof(AssetSettings)),
            new("UISettings", "UISettings.asset", "UI", typeof(UISettings)),
            new("AudioSettings", "AudioSettings.asset", "Audio", typeof(AudioSettings)),
            new("SaveSettings", "SaveSettings.asset", "Save", typeof(SaveSettings)),
            new("PoolSettings", "PoolSettings.asset", "Pool", typeof(PoolSettings)),
            new("LogSettings", "LogSettings.asset", "Log", typeof(LogSettings)),
            new("ConfigSettings", "ConfigSettings.asset", "Config", typeof(ConfigSettings)),
        };

        [MenuItem(MenuPath, priority = 400)]
        public static void ShowInitializerDialog()
        {
            var lastPath = EditorPrefs.GetString(EditorPrefsKey, DefaultOutputRoot);

            var selectedPath = EditorUtility.OpenFolderPanel("选择 Settings 输出目录", lastPath, "");
            if (string.IsNullOrEmpty(selectedPath))
                return;

            // 转为 Assets 相对路径
            var assetPath = ToRelativePath(selectedPath);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("路径错误", "请选择项目 Assets 目录下的文件夹", "确定");
                return;
            }

            EditorPrefs.SetString(EditorPrefsKey, selectedPath);
            GenerateAll(assetPath);
        }

        /// <summary>
        ///     在指定路径下生成所有 Settings 资产
        /// </summary>
        /// <param name="outputRoot">Assets 相对路径（如 "Assets/Resources"）</param>
        public static void GenerateAll(string outputRoot)
        {
            if (!Directory.Exists(outputRoot))
                Directory.CreateDirectory(outputRoot);

            var created = new List<string>();
            var skipped = new List<string>();

            // 1. 生成子 Settings
            var subAssets = new Dictionary<string, ScriptableObject>();
            foreach (var info in SubSettings)
            {
                var filePath = Path.Combine(outputRoot, info.FileName).Replace('\\', '/');
                var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(filePath);

                if (existing != null)
                {
                    skipped.Add($"{info.TypeName}（已存在）");
                    subAssets[info.FieldName] = existing;
                    continue;
                }

                var instance = ScriptableObject.CreateInstance(info.Type);
                AssetDatabase.CreateAsset(instance, filePath);
                subAssets[info.FieldName] = instance;
                created.Add(info.TypeName);
            }

            // 2. 生成 FrameworkSettings
            var frameworkPath = "Assets/Resources/" + FrameworkSettings.DefaultPath + ".asset";
            var frameworkAsset = AssetDatabase.LoadAssetAtPath<FrameworkSettings>(frameworkPath);

            if (frameworkAsset == null)
            {
                frameworkAsset = ScriptableObject.CreateInstance<FrameworkSettings>();
                AssetDatabase.CreateAsset(frameworkAsset, frameworkPath);
                created.Add("FrameworkSettings");
            }
            else
            {
                skipped.Add("FrameworkSettings（已存在）");
            }

            // 3. 建立引用关系（仅对 FrameworkSettings 中尚未赋值的字段）
            var dirty = false;
            var serialized = new SerializedObject(frameworkAsset);

            foreach (var info in SubSettings)
            {
                var prop = serialized.FindProperty(info.FieldName);
                if (prop != null && prop.objectReferenceValue == null &&
                    subAssets.TryGetValue(info.FieldName, out var subAsset))
                {
                    prop.objectReferenceValue = subAsset;
                    dirty = true;
                }
            }

            if (dirty)
            {
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(frameworkAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. 在 Project 窗口中高亮 FrameworkSettings
            EditorGUIUtility.PingObject(frameworkAsset);

            // 5. 输出结果
            var message = BuildResultMessage(outputRoot, created, skipped);
            Debug.Log($"[CFramework] 框架设置初始化完成\n{message}");
            EditorUtility.DisplayDialog("初始化完成", message, "确定");
        }

        /// <summary>
        ///     将绝对路径转换为 Assets 相对路径
        /// </summary>
        private static string ToRelativePath(string absolutePath)
        {
            var dataPath = Application.dataPath.Replace('\\', '/');
            absolutePath = absolutePath.Replace('\\', '/');

            if (absolutePath.StartsWith(dataPath))
                return "Assets" + absolutePath.Substring(dataPath.Length);

            return null;
        }

        /// <summary>
        ///     构建结果消息
        /// </summary>
        private static string BuildResultMessage(string outputRoot, List<string> created, List<string> skipped)
        {
            var lines = new List<string>();
            lines.Add($"输出目录：{outputRoot}");
            lines.Add("");

            if (created.Count > 0)
            {
                lines.Add($"新建 {created.Count} 个资产：");
                foreach (var name in created)
                    lines.Add($"  + {name}");
            }

            if (skipped.Count > 0)
            {
                lines.Add("");
                lines.Add($"跳过 {skipped.Count} 个（已存在）：");
                foreach (var name in skipped)
                    lines.Add($"  - {name}");
            }

            return string.Join("\n", lines);
        }
    }
}
#endif