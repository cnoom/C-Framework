#if !ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CFramework.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Windows.Config
{
    /// <summary>
    ///     配置创建器窗口（默认实现，不依赖 Odin）
    /// </summary>
    public sealed class ConfigCreatorWindow : EditorWindow
    {
        #region 菜单项

        [MenuItem("CFramework/创建配置表", false, 101)]
        public static void OpenWindow()
        {
            var window = GetWindow<ConfigCreatorWindow>("创建配置表");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        #endregion

        #region 字段

        private string configName = "NewConfig";
        private string configNamespace = "Game.Configs";
        private string configOutputPath = EditorPaths.ConfigScripts;
        private string dataNamespace = "Game.Configs";
        private string dataOutputPath = EditorPaths.ConfigScripts;
        private string outputAssetPath = EditorPaths.EditorConfigs;
        private bool openGeneratedScript = true;
        private bool autoCreateAsset = true;
        private string keyType = "int";
        private string valueTypeName = "ItemData";
        private List<ValueField> valueFields = new()
        {
            new ValueField { fieldName = "id", fieldType = "int", isKeyField = true },
            new ValueField { fieldName = "name", fieldType = "string" }
        };

        // 编辑器状态
        private Vector2 _scrollPos;
        private int _selectedFieldIndex = -1;

        private static readonly string[] KeyTypeOptions =
        {
            "int", "string", "long", "byte", "short", "uint", "ulong", "ushort"
        };

        private static readonly string[] FieldTypeOptions =
        {
            "int", "float", "string", "bool", "long", "double",
            "Vector2", "Vector3", "Vector4", "Color",
            "GameObject", "Transform", "Sprite", "Texture", "AudioClip"
        };

        #endregion

        #region 偏好设置键

        private const string PREF_CONFIG_NAMESPACE = "CFramework.ConfigCreator.ConfigNamespace";
        private const string PREF_CONFIG_OUTPUT_PATH = "CFramework.ConfigCreator.ConfigOutputPath";
        private const string PREF_DATA_NAMESPACE = "CFramework.ConfigCreator.DataNamespace";
        private const string PREF_DATA_OUTPUT_PATH = "CFramework.ConfigCreator.DataOutputPath";
        private const string PREF_ASSET_OUTPUT_PATH = "CFramework.ConfigCreator.AssetOutputPath";
        private const string PREF_KEY_TYPE = "CFramework.ConfigCreator.KeyType";

        #endregion

        #region 生命周期

        private void OnEnable()
        {
            LoadPreferences();
        }

        private void OnDisable()
        {
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

        #region GUI

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawBasicConfig();
            DrawTypeConfig();
            DrawFieldList();
            DrawOutputConfig();
            DrawPreview();
            DrawButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawBasicConfig()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("基础配置", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            configName = EditorGUILayout.TextField("配置表名称", configName);
            if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(configName))
            {
                if (configName.EndsWith("Config"))
                    valueTypeName = configName.Substring(0, configName.Length - "Config".Length) + "Data";
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("配置表设置", EditorStyles.boldLabel);
            configNamespace = EditorGUILayout.TextField("命名空间", configNamespace);
            configOutputPath = EditorGUILayout.TextField("输出目录", configOutputPath);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("数据类设置", EditorStyles.boldLabel);
            dataNamespace = EditorGUILayout.TextField("命名空间", dataNamespace);
            dataOutputPath = EditorGUILayout.TextField("输出目录", dataOutputPath);

            EditorGUI.indentLevel--;
        }

        private void DrawTypeConfig()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("类型配置", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var keyIndex = Array.IndexOf(KeyTypeOptions, keyType);
            if (keyIndex < 0) keyIndex = 0;
            keyIndex = EditorGUILayout.Popup("键类型", keyIndex, KeyTypeOptions);
            keyType = KeyTypeOptions[keyIndex];

            valueTypeName = EditorGUILayout.TextField("值类型名称", valueTypeName);

            EditorGUI.indentLevel--;
        }

        private void DrawFieldList()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("值类型字段", EditorStyles.boldLabel);

            // 字段列表
            for (var i = 0; i < valueFields.Count; i++)
            {
                var field = valueFields[i];
                var isSelected = i == _selectedFieldIndex;

                var bgStyle = isSelected
                    ? new GUIStyle("HelpBox") { padding = new RectOffset(8, 8, 4, 4) }
                    : new GUIStyle("HelpBox") { padding = new RectOffset(8, 8, 4, 4) };

                var rect = EditorGUILayout.BeginVertical(bgStyle);

                if (isSelected)
                {
                    EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), new Color(0.24f, 0.49f, 0.75f, 0.2f));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    var typeIndex = Array.IndexOf(FieldTypeOptions, field.fieldType);
                    if (typeIndex < 0) typeIndex = 0;

                    field.isKeyField = EditorGUILayout.ToggleLeft("主键", field.isKeyField, GUILayout.Width(50));
                    field.fieldName = EditorGUILayout.TextField(field.fieldName);
                    typeIndex = EditorGUILayout.Popup(typeIndex, FieldTypeOptions, GUILayout.Width(80));
                    field.fieldType = FieldTypeOptions[typeIndex];

                    if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        valueFields.RemoveAt(i);
                        if (_selectedFieldIndex >= valueFields.Count) _selectedFieldIndex = valueFields.Count - 1;
                        break;
                    }
                }

                field.description = EditorGUILayout.TextField("描述", field.description);

                EditorGUILayout.EndVertical();

                // 点击选择
                var evt = Event.current;
                if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
                {
                    _selectedFieldIndex = i;
                    evt.Use();
                }
            }

            // 添加字段按钮
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ 添加字段", GUILayout.Width(100)))
                {
                    valueFields.Add(new ValueField { fieldName = $"field{valueFields.Count}", fieldType = "int" });
                    _selectedFieldIndex = valueFields.Count - 1;
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawOutputConfig()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("资源设置", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            outputAssetPath = EditorGUILayout.TextField("资源输出目录", outputAssetPath);
            openGeneratedScript = EditorGUILayout.Toggle("打开生成的脚本", openGeneratedScript);
            autoCreateAsset = EditorGUILayout.Toggle("自动创建资产", autoCreateAsset);

            EditorGUI.indentLevel--;
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("代码预览", EditorStyles.boldLabel);

            // 配置表类预览
            EditorGUILayout.LabelField("配置表类:", EditorStyles.miniBoldLabel);
            var configCode = GenerateConfigClassCode();
            var configStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false,
                fontSize = 11
            };
            EditorGUILayout.TextArea(configCode, configStyle, GUILayout.Height(80));

            EditorGUILayout.Space(4);

            // 数据类预览
            EditorGUILayout.LabelField("数据类:", EditorStyles.miniBoldLabel);
            var dataCode = GenerateDataClassCode();
            EditorGUILayout.TextArea(dataCode, configStyle, GUILayout.Height(120));
        }

        private void DrawButtons()
        {
            EditorGUILayout.Space(8);

            var canGenerate = !string.IsNullOrEmpty(configName) &&
                             !string.IsNullOrEmpty(valueTypeName) &&
                             valueFields.Count > 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(!canGenerate);

                if (GUILayout.Button("仅生成代码", GUILayout.Width(120), GUILayout.Height(30)))
                {
                    GenerateCodeOnly();
                }

                if (GUILayout.Button("生成代码并创建资产", GUILayout.Width(160), GUILayout.Height(30)))
                {
                    GenerateCodeAndAsset();
                }

                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(8);
        }

        #endregion

        #region 生成逻辑

        private void GenerateCodeOnly()
        {
            try
            {
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

        private void GenerateCodeAndAsset()
        {
            try
            {
                SavePreferences();
                GenerateScriptFiles();

                if (autoCreateAsset)
                {
                    CreateConfigAsset();
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

        private void GenerateScriptFiles()
        {
            if (!Directory.Exists(configOutputPath)) Directory.CreateDirectory(configOutputPath);
            if (!Directory.Exists(dataOutputPath)) Directory.CreateDirectory(dataOutputPath);

            var dataCode = GenerateDataClassCode();
            var dataFilePath = Path.Combine(dataOutputPath, $"{valueTypeName}.cs");
            File.WriteAllText(dataFilePath, dataCode, Encoding.UTF8);

            var configCode = GenerateConfigClassCode();
            var configFilePath = Path.Combine(configOutputPath, $"{configName}.cs");
            File.WriteAllText(configFilePath, configCode, Encoding.UTF8);

            Debug.Log($"[ConfigCreator] 生成文件：\n{dataFilePath}\n{configFilePath}");

            if (openGeneratedScript)
            {
                AssetDatabase.Refresh();
                var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataFilePath);
                var configAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(configFilePath);
                if (dataAsset != null) AssetDatabase.OpenAsset(dataAsset);
                if (configAsset != null) AssetDatabase.OpenAsset(configAsset);
            }
        }

        private void CreateConfigAsset()
        {
            if (!Directory.Exists(outputAssetPath)) Directory.CreateDirectory(outputAssetPath);

            ConfigAssetCreator.RegisterPendingAsset(configName, configNamespace, outputAssetPath);
            AssetDatabase.Refresh();
            Debug.Log("[ConfigCreator] 脚本已生成，等待编译完成后自动创建资产...");
        }

        private string GenerateDataClassCode()
        {
            var sb = new StringBuilder();
            var keyField = valueFields.Find(f => f.isKeyField);
            if (keyField == null && valueFields.Count > 0) keyField = valueFields[0];

            sb.AppendLine("using System;");
            sb.AppendLine("using CFramework;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(dataNamespace))
            {
                sb.AppendLine($"namespace {dataNamespace}");
                sb.AppendLine("{");
            }

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {valueTypeName} 数据结构");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public sealed class {valueTypeName} : IConfigItem<{keyType}>");
            sb.AppendLine("    {");

            foreach (var field in valueFields)
            {
                if (!string.IsNullOrEmpty(field.description))
                {
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// {field.description}");
                    sb.AppendLine("        /// </summary>");
                }

                sb.Append($"        public {field.fieldType} {field.fieldName}");

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

            if (keyField != null)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// 配置数据主键");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public {keyType} Key => {keyField.fieldName};");
                sb.AppendLine();
            }

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 克隆当前对象");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {valueTypeName} Clone()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new {valueTypeName}");
            sb.AppendLine("            {");

            for (var i = 0; i < valueFields.Count; i++)
            {
                var field = valueFields[i];
                sb.Append($"                {field.fieldName} = {field.fieldName}");
                sb.AppendLine(i < valueFields.Count - 1 ? "," : "");
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

            sb.AppendLine("using CFramework;");
            sb.AppendLine("using UnityEngine;");

            if (!string.IsNullOrEmpty(dataNamespace) && dataNamespace != configNamespace)
                sb.AppendLine($"using {dataNamespace};");

            sb.AppendLine();

            if (!string.IsNullOrEmpty(configNamespace))
            {
                sb.AppendLine($"namespace {configNamespace}");
                sb.AppendLine("{");
            }

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {configName} 配置表");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    [CreateAssetMenu(fileName = \"{configName}\", menuName = \"Game/Config/{configName}\")]");
            sb.AppendLine($"    public sealed class {configName} : ConfigTable<{keyType}, {valueTypeName}>");
            sb.AppendLine("    {");
            sb.AppendLine("        // 数据在 Inspector 中配置");
            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(configNamespace)) sb.AppendLine("}");

            return sb.ToString();
        }

        private bool IsNumericType(string type)
        {
            return type == "int" || type == "float" || type == "long" ||
                   type == "double" || type == "byte" || type == "short" ||
                   type == "uint" || type == "ulong" || type == "ushort";
        }

        #endregion

        #region 数据类

        [Serializable]
        public sealed class ValueField
        {
            public string fieldName;
            public string fieldType = "int";
            public bool isKeyField;
            public string description;
        }

        #endregion
    }
}
#endif
