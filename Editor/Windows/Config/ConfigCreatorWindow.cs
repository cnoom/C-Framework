#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        [MenuItem("CFramework/创建配置表", priority = 101)]
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

        [Serializable]
        public sealed class ValueField
        {
            [LabelText("字段名")] [Tooltip("字段名称")] [Required]
            public string fieldName;

            [LabelText("类型")] [ValueDropdown(nameof(FieldTypeOptions))]
            public string fieldType = "int";

            [LabelText("主键")] [Tooltip("是否作为主键字段")]
            public bool isKeyField;

            [LabelText("描述")] [Tooltip("字段描述（用于注释）")] [TextArea(1, 2)]
            public string description;

            private IEnumerable<string> FieldTypeOptions = new[]
            {
                "int",
                "float",
                "string",
                "bool",
                "long",
                "double",
                "Vector2",
                "Vector3",
                "Vector4",
                "Color",
                "GameObject",
                "Transform",
                "Sprite",
                "Texture",
                "AudioClip"
            };
        }

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
            // 确保目录存在
            if (!Directory.Exists(configOutputPath)) Directory.CreateDirectory(configOutputPath);

            if (!Directory.Exists(dataOutputPath)) Directory.CreateDirectory(dataOutputPath);

            // 生成数据类
            var dataCode = GenerateDataClassCode();
            var dataFilePath = Path.Combine(dataOutputPath, $"{valueTypeName}.cs");
            File.WriteAllText(dataFilePath, dataCode, Encoding.UTF8);

            // 生成配置表类
            var configCode = GenerateConfigClassCode();
            var configFilePath = Path.Combine(configOutputPath, $"{configName}.cs");
            File.WriteAllText(configFilePath, configCode, Encoding.UTF8);

            Debug.Log($"[ConfigCreator] 生成文件：\n{dataFilePath}\n{configFilePath}");

            // 打开生成的脚本
            if (openGeneratedScript)
            {
                AssetDatabase.Refresh();

                var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataFilePath);
                var configAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(configFilePath);

                if (dataAsset != null)
                    AssetDatabase.OpenAsset(dataAsset);
                if (configAsset != null)
                    AssetDatabase.OpenAsset(configAsset);
            }
        }

        private string GenerateDataClassCode()
        {
            var sb = new StringBuilder();

            // 查找主键字段
            var keyField = valueFields.Find(f => f.isKeyField);
            if (keyField == null && valueFields.Count > 0) keyField = valueFields[0]; // 默认第一个字段为主键

            // Using 语句
            sb.AppendLine("using System;");
            sb.AppendLine("using CFramework;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            // 命名空间
            if (!string.IsNullOrEmpty(dataNamespace))
            {
                sb.AppendLine($"namespace {dataNamespace}");
                sb.AppendLine("{");
            }

            // 数据类
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {valueTypeName} 数据结构");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public sealed class {valueTypeName} : IConfigItem<{keyType}>");
            sb.AppendLine("    {");

            // 字段
            foreach (var field in valueFields)
            {
                if (!string.IsNullOrEmpty(field.description))
                {
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// {field.description}");
                    sb.AppendLine("        /// </summary>");
                }

                sb.Append($"        public {field.fieldType} {field.fieldName}");

                // 默认值
                if (field.fieldType == "string")
                    sb.AppendLine(" = \"\";");
                else if (field.fieldType == "bool")
                    sb.AppendLine(" = false;");
                else if (IsNumericType(field.fieldType))
                    sb.AppendLine(" = 0;");
                else
                    sb.AppendLine(";");

                sb.AppendLine();
            }

            // Key 属性
            if (keyField != null)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// 配置数据主键");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public {keyType} Key => {keyField.fieldName};");
                sb.AppendLine();
            }

            // 克隆方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 克隆当前对象");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <returns>对象的深拷贝</returns>");
            sb.AppendLine($"        public {valueTypeName} Clone()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new {valueTypeName}");
            sb.AppendLine("            {");

            // 字段赋值
            for (var i = 0; i < valueFields.Count; i++)
            {
                var field = valueFields[i];
                sb.Append($"                {field.fieldName} = {field.fieldName}");

                if (i < valueFields.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(dataNamespace)) sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateConfigClassCode()
        {
            var sb = new StringBuilder();

            // Using 语句
            sb.AppendLine("using CFramework;");
            sb.AppendLine("using UnityEngine;");

            // 如果数据类在不同命名空间，添加 using
            if (!string.IsNullOrEmpty(dataNamespace) && dataNamespace != configNamespace)
                sb.AppendLine($"using {dataNamespace};");

            sb.AppendLine();

            // 命名空间
            if (!string.IsNullOrEmpty(configNamespace))
            {
                sb.AppendLine($"namespace {configNamespace}");
                sb.AppendLine("{");
            }

            // 配置表类
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {configName} 配置表");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    [CreateAssetMenu(fileName = \"{configName}\", menuName = \"Game/Config/{configName}\")]");
            sb.AppendLine(
                $"    public sealed class {configName} : ConfigTableAsset<{keyType}, {valueTypeName}>");
            sb.AppendLine("    {");
            sb.AppendLine("        // 数据在 Inspector 中配置");
            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(configNamespace)) sb.AppendLine("}");

            return sb.ToString();
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

        private bool IsNumericType(string type)
        {
            return type == "int" || type == "float" || type == "long" ||
                   type == "double" || type == "byte" || type == "short" ||
                   type == "uint" || type == "ulong" || type == "ushort";
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