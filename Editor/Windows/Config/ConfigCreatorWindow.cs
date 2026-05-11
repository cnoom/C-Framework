#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.IO;
using CFramework.Editor.Generators;
using CFramework.Editor.Utilities;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Windows.Config
{
    /// <summary>
    ///     配置创建器窗口
    /// </summary>
    public sealed class ConfigCreatorWindow : OdinEditorWindow
    {
        #region 菜单项

                /// <summary>
        ///     打开窗口（由 Dashboard 调用）
        /// </summary>
        public static void OpenWindow()
        {
            var window = GetWindow<ConfigCreatorWindow>("创建配置表");
            window.position = new Rect(200, 200, 600, 700);
            window.Show();
        }

        #endregion

        #region 字段

        [TitleGroup("基础配置", Subtitle = "配置表基本信息")]
        [LabelText("配置表名称")]
        [Tooltip("配置表的类名，例如：ItemConfig")]
        [Required("配置表名称不能为空")]
        [OnValueChanged(nameof(OnConfigNameChanged))]
        public string configName = "NewConfig";

        [TitleGroup("配置表设置")] [LabelText("命名空间")] [Tooltip("配置表所在的命名空间")]
        public string configNamespace = "Game.Configs";

        [TitleGroup("配置表设置")]
        [LabelText("输出目录")]
        [FolderPath(RequireExistingPath = true)]
        [Tooltip("配置表脚本文件存放目录")]
        [OnValueChanged(nameof(SavePreferences))]
        public string configOutputPath = EditorPaths.ConfigScripts;

        [TitleGroup("数据类设置")] [LabelText("命名空间")] [Tooltip("数据类所在的命名空间")]
        public string dataNamespace = "Game.Configs";

        [TitleGroup("数据类设置")]
        [LabelText("输出目录")]
        [FolderPath(RequireExistingPath = true)]
        [Tooltip("数据类脚本文件存放目录")]
        [OnValueChanged(nameof(SavePreferences))]
        public string dataOutputPath = EditorPaths.ConfigScripts;

        [TitleGroup("资源设置")]
        [LabelText("资源输出目录")]
        [FolderPath(RequireExistingPath = true)]
        [Tooltip("创建的配置资产存放目录")]
        [OnValueChanged(nameof(SavePreferences))]
        public string outputAssetPath = EditorPaths.EditorConfigs;

        [TitleGroup("资源设置")] [LabelText("打开生成的脚本")]
        public bool openGeneratedScript = true;

        [TitleGroup("资源设置")] [LabelText("自动创建资产")] [Tooltip("是否在生成代码后自动创建配置资产文件")]
        public bool autoCreateAsset = true;

        #endregion

        #region 偏好设置键

        private const string PREF_CONFIG_NAMESPACE = "CFramework.ConfigCreator.ConfigNamespace";
        private const string PREF_CONFIG_OUTPUT_PATH = "CFramework.ConfigCreator.ConfigOutputPath";
        private const string PREF_DATA_NAMESPACE = "CFramework.ConfigCreator.DataNamespace";
        private const string PREF_DATA_OUTPUT_PATH = "CFramework.ConfigCreator.DataOutputPath";
        private const string PREF_ASSET_OUTPUT_PATH = "CFramework.ConfigCreator.AssetOutputPath";
        private const string PREF_KEY_TYPE = "CFramework.ConfigCreator.KeyType";

        #endregion

        #region 初始化

        protected override void OnEnable()
        {
            base.OnEnable();
            LoadPreferences();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SavePreferences();
        }

        private void LoadPreferences()
        {
            configNamespace = EditorPrefs.GetString(PREF_CONFIG_NAMESPACE, "Game.Configs");
            configOutputPath = EditorPrefs.GetString(PREF_CONFIG_OUTPUT_PATH, EditorPaths.ConfigScripts);
            dataNamespace = EditorPrefs.GetString(PREF_DATA_NAMESPACE, "Game.Configs");
            dataOutputPath = EditorPrefs.GetString(PREF_DATA_OUTPUT_PATH, EditorPaths.ConfigScripts);
            outputAssetPath = EditorPrefs.GetString(PREF_ASSET_OUTPUT_PATH, EditorPaths.EditorConfigs);
            keyType = EditorPrefs.GetString(PREF_KEY_TYPE, "int");
        }

        private void SavePreferences()
        {
            EditorPrefs.SetString(PREF_CONFIG_NAMESPACE, configNamespace);
            EditorPrefs.SetString(PREF_CONFIG_OUTPUT_PATH, configOutputPath);
            EditorPrefs.SetString(PREF_DATA_NAMESPACE, dataNamespace);
            EditorPrefs.SetString(PREF_DATA_OUTPUT_PATH, dataOutputPath);
            EditorPrefs.SetString(PREF_ASSET_OUTPUT_PATH, outputAssetPath);
            EditorPrefs.SetString(PREF_KEY_TYPE, keyType);
        }

        #endregion

        #region 键值类型配置

        [TitleGroup("类型配置")]
        [LabelText("键类型")]
        [ValueDropdown(nameof(KeyTypeOptions))]
        [OnValueChanged(nameof(OnKeyTypeChanged))]
        public string keyType = "int";

        private IEnumerable<string> KeyTypeOptions = new[]
        {
            "int",
            "string",
            "long",
            "byte",
            "short",
            "uint",
            "ulong",
            "ushort"
        };

        [TitleGroup("类型配置")]
        [LabelText("值类型名称")]
        [Tooltip("值类型的类名，例如：ItemData")]
        [Required("值类型名称不能为空")]
        [OnValueChanged(nameof(OnValueTypeChanged))]
        public string valueTypeName = "ItemData";

        [TitleGroup("类型配置")] [LabelText("值类型字段")] [ListDrawerSettings(ShowPaging = false, DraggableItems = true)]
        public List<ValueField> valueFields = new()
        {
            new ValueField { fieldName = "id", fieldType = "int", isKeyField = true },
            new ValueField { fieldName = "name", fieldType = "string" }
        };

        #endregion

        #region 预览

        [PropertyOrder(100)]
        [FoldoutGroup("代码预览")]
        [LabelText("生成的配置表类")]
        [DisplayAsString(UnityEngine.TextAlignment.Left, Overflow = false)]
        [HideLabel]
        [ShowInInspector]
        private string PreviewCode => GenerateConfigClassCode();

        [PropertyOrder(101)]
        [FoldoutGroup("代码预览")]
        [LabelText("生成的数据类")]
        [DisplayAsString(UnityEngine.TextAlignment.Left, Overflow = false)]
        [HideLabel]
        [ShowInInspector]
        private string PreviewDataCode => GenerateDataClassCode();

        #endregion

        #region 操作按钮

        [PropertyOrder(200)]
        [HorizontalGroup("操作", 0.5f)]
        [Button("生成代码", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        [EnableIf(nameof(CanGenerate))]
        private void GenerateCode()
        {
            try
            {
                // 保存偏好设置
                SavePreferences();

                // 生成脚本文件
                GenerateScriptFiles();

                // 创建配置资产
                if (autoCreateAsset)
                {
                    CreateConfigAsset();

                    // 注意：资产将在编译完成后自动创建
                    EditorUtility.DisplayDialog("代码生成成功",
                        "配置表代码已生成！\n\n" +
                        $"配置表路径：{configOutputPath}/{configName}.cs\n" +
                        $"数据类路径：{dataOutputPath}/{valueTypeName}.cs\n\n" +
                        "资产将在编译完成后自动创建于：\n" +
                        $"{outputAssetPath}/{configName}.asset",
                        "确定");
                }
                else
                {
                    AssetDatabase.Refresh();

                    EditorUtility.DisplayDialog("成功",
                        "配置表创建成功！\n\n" +
                        $"配置表路径：{configOutputPath}/{configName}.cs\n" +
                        $"数据类路径：{dataOutputPath}/{valueTypeName}.cs\n\n" +
                        "资产未自动创建，请手动创建",
                        "确定");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"创建失败：{ex.Message}", "确定");
                Debug.LogError($"[ConfigCreator] 创建失败: {ex}");
            }
        }

        [PropertyOrder(201)]
        [HorizontalGroup("操作", 0.5f)]
        [Button("仅生成代码", ButtonSizes.Large)]
        [GUIColor(0.8f, 1f, 0.4f)]
        [EnableIf(nameof(CanGenerate))]
        private void GenerateCodeOnly()
        {
            try
            {
                // 保存偏好设置
                SavePreferences();

                GenerateScriptFiles();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("成功",
                    "代码生成成功！\n\n" +
                    $"配置表路径：{configOutputPath}/{configName}.cs\n" +
                    $"数据类路径：{dataOutputPath}/{valueTypeName}.cs",
                    "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"生成失败：{ex.Message}", "确定");
                Debug.LogError($"[ConfigCreator] 生成失败: {ex}");
            }
        }

        #endregion

        #region 生成逻辑

        private bool CanGenerate()
        {
            return !string.IsNullOrEmpty(configName) &&
                   !string.IsNullOrEmpty(valueTypeName) &&
                   valueFields.Count > 0;
        }

        private void GenerateScriptFiles()
        {
            ConfigCodeGenerator.WriteScriptFiles(
                configOutputPath,
                dataOutputPath,
                configName,
                valueTypeName,
                configNamespace,
                dataNamespace,
                keyType,
                valueFields,
                openGeneratedScript);
        }

        private string GenerateDataClassCode()
        {
            return ConfigCodeGenerator.GenerateDataClassCode(
                valueTypeName, keyType, dataNamespace, valueFields);
        }

        private string GenerateConfigClassCode()
        {
            return ConfigCodeGenerator.GenerateConfigClassCode(
                configName, configNamespace, dataNamespace,
                keyType, valueTypeName);
        }

        private void CreateConfigAsset()
        {
            // 确保目录存在
            if (!Directory.Exists(outputAssetPath)) Directory.CreateDirectory(outputAssetPath);

            // 注册待创建的资产信息，等待编译完成后创建
            ConfigAssetCreator.RegisterPendingAsset(
                configName,
                configNamespace,
                outputAssetPath
            );

            // 刷新资产数据库触发编译
            AssetDatabase.Refresh();

            Debug.Log("[ConfigCreator] 脚本已生成，等待编译完成后自动创建资产...");
        }

        #endregion

        #region 回调

        private void OnConfigNameChanged()
        {
            if (!string.IsNullOrEmpty(configName))
                // 自动生成值类型名称
                if (configName.EndsWith("Config"))
                    valueTypeName = configName.Substring(0, configName.Length - "Config".Length) + "Data";
        }

        private void OnKeyTypeChanged()
        {
            // 更新主键字段类型
            if (valueFields.Count > 0) valueFields[0].fieldType = keyType;
            SavePreferences();
        }

        private void OnValueTypeChanged()
        {
            // 可以在此添加逻辑
        }

        #endregion
    }
}
#endif