using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CFramework.Editor.Utilities;
using CNoom.UnityTool.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows
{
    /// <summary>
    ///     Dashboard 配置创建 Tab 的内容构建器
    ///     从 ConfigCreatorWindowDefault 提取，嵌入 Dashboard 使用
    /// </summary>
    public class DashboardConfigCreatorTab
    {
        #region 数据字段

        private string _configName = "NewConfig";
        private string _configNamespace = "Game.Configs";
        private string _configOutputPath = EditorPaths.ConfigScripts;
        private string _dataNamespace = "Game.Configs";
        private string _dataOutputPath = EditorPaths.ConfigScripts;
        private string _outputAssetPath = EditorPaths.EditorConfigs;
        private bool _openGeneratedScript = true;
        private bool _autoCreateAsset = true;
        private string _keyType = "int";
        private string _valueTypeName = "ItemData";
        private readonly List<ValueField> _valueFields = new()
        {
            new ValueField { fieldName = "id", fieldType = "int", isKeyField = true },
            new ValueField { fieldName = "name", fieldType = "string" }
        };

        #endregion

        #region UIToolkit 控件引用

        private TextField _configNameField;
        private TextField _configNamespaceField;
        private TextField _configOutputField;
        private TextField _dataNamespaceField;
        private TextField _dataOutputField;
        private PopupField<string> _keyTypePopup;
        private TextField _valueTypeNameField;
        private ListView _fieldListView;
        private TextField _assetPathField;
        private Toggle _openScriptToggle;
        private Toggle _autoAssetToggle;
        private TextField _configPreviewField;
        private TextField _dataPreviewField;
        private Button _generateCodeBtn;
        private Button _generateAllBtn;
        private int _selectedFieldIndex = -1;

        #endregion

        #region 常量

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

        private const string PREF_CONFIG_NAMESPACE = "CFramework.ConfigCreator.ConfigNamespace";
        private const string PREF_CONFIG_OUTPUT_PATH = "CFramework.ConfigCreator.ConfigOutputPath";
        private const string PREF_DATA_NAMESPACE = "CFramework.ConfigCreator.DataNamespace";
        private const string PREF_DATA_OUTPUT_PATH = "CFramework.ConfigCreator.DataOutputPath";
        private const string PREF_ASSET_OUTPUT_PATH = "CFramework.ConfigCreator.AssetOutputPath";
        private const string PREF_KEY_TYPE = "CFramework.ConfigCreator.KeyType";

        #endregion

        #region 公开接口

        /// <summary>
        ///     加载偏好设置
        /// </summary>
        public void LoadPreferences()
        {
            _configNamespace = EditorPrefs.GetString(PREF_CONFIG_NAMESPACE, "Game.Configs");
            _configOutputPath = EditorPrefs.GetString(PREF_CONFIG_OUTPUT_PATH, EditorPaths.ConfigScripts);
            _dataNamespace = EditorPrefs.GetString(PREF_DATA_NAMESPACE, "Game.Configs");
            _dataOutputPath = EditorPrefs.GetString(PREF_DATA_OUTPUT_PATH, EditorPaths.ConfigScripts);
            _outputAssetPath = EditorPrefs.GetString(PREF_ASSET_OUTPUT_PATH, EditorPaths.EditorConfigs);
            _keyType = EditorPrefs.GetString(PREF_KEY_TYPE, "int");
        }

        /// <summary>
        ///     保存偏好设置
        /// </summary>
        public void SavePreferences()
        {
            if (_configNamespaceField == null) return;

            EditorPrefs.SetString(PREF_CONFIG_NAMESPACE, _configNamespaceField.value);
            EditorPrefs.SetString(PREF_CONFIG_OUTPUT_PATH, _configOutputField.value);
            EditorPrefs.SetString(PREF_DATA_NAMESPACE, _dataNamespaceField.value);
            EditorPrefs.SetString(PREF_DATA_OUTPUT_PATH, _dataOutputField.value);
            EditorPrefs.SetString(PREF_ASSET_OUTPUT_PATH, _assetPathField.value);
            EditorPrefs.SetString(PREF_KEY_TYPE, _keyTypePopup.value);
        }

        /// <summary>
        ///     创建 Tab 内容
        /// </summary>
        public VisualElement CreateContent()
        {
            var container = new ScrollView(ScrollViewMode.Vertical);
            container.AddToClassList("tab-scroll");
            container.style.flexGrow = 1;
            // 强制子元素宽度等于 viewport 宽度，防止横向溢出
            container.contentContainer.style.alignItems = Align.Stretch;

            container.Add(CreateBasicConfigSection());
            container.Add(CreateTypeConfigSection());
            container.Add(CreateFieldListSection());
            container.Add(CreateOutputConfigSection());
            container.Add(CreatePreviewSection());
            container.Add(CreateButtonSection());

            // 延迟绑定初始值
            EditorApplication.delayCall += () =>
            {
                BindInitialValues();
                UpdatePreview();
            };

            return container;
        }

        #endregion

        #region UI 构建

        private VisualElement CreateBasicConfigSection()
        {
            var section = CreateSection("基础配置");

            section.Add(CreateLabeledField("配置表名称",
                out _configNameField,
                OnConfigNameChanged));

            var subLabel1 = new Label("配置表设置");
            subLabel1.AddToClassList("sub-label");
            section.Add(subLabel1);
            section.Add(CreateIndentedField("命名空间", out _configNamespaceField));
            section.Add(CreateIndentedField("输出目录", out _configOutputField));

            var subLabel2 = new Label("数据类设置");
            subLabel2.AddToClassList("sub-label");
            section.Add(subLabel2);
            section.Add(CreateIndentedField("命名空间", out _dataNamespaceField));
            section.Add(CreateIndentedField("输出目录", out _dataOutputField));

            return section;
        }

        private VisualElement CreateTypeConfigSection()
        {
            var section = CreateSection("类型配置");

            // 键类型下拉框
            var keyRow = CreateLabeledField("键类型", out _, null);
            var keyPopupContainer = keyRow.Q<VisualElement>("field-container");

            _keyTypePopup = new PopupField<string>("", new List<string>(KeyTypeOptions), 0);
            _keyTypePopup.AddToClassList("popup-field");
            _keyTypePopup.RegisterValueChangedCallback(evt => { _keyType = evt.newValue; });
            keyPopupContainer?.Clear();
            keyPopupContainer?.Add(_keyTypePopup);

            section.Add(keyRow);

            section.Add(CreateLabeledField("值类型名称",
                out _valueTypeNameField,
                evt => { _valueTypeName = evt.newValue; }));

            return section;
        }

        private VisualElement CreateFieldListSection()
        {
            var section = CreateSection("值类型字段");

            _fieldListView = new ListView
            {
                makeItem = () => new FieldItemElement(FieldTypeOptions),
                bindItem = (element, index) =>
                {
                    if (element is FieldItemElement itemElem)
                    {
                        itemElem.SetData(_valueFields[index], index == _selectedFieldIndex,
                            onRemove: () =>
                            {
                                _valueFields.RemoveAt(index);
                                if (_selectedFieldIndex >= _valueFields.Count)
                                    _selectedFieldIndex = _valueFields.Count - 1;
                                _fieldListView.RefreshItems();
                                UpdatePreview();
                            },
                            onSelect: () =>
                            {
                                _selectedFieldIndex = index;
                                _fieldListView.RefreshItems();
                            },
                            onChanged: () => UpdatePreview()
                        );
                    }
                },
                itemsSource = _valueFields,
                selectionType = SelectionType.None,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                fixedItemHeight = 56,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };
            _fieldListView.AddToClassList("field-list");

            section.Add(_fieldListView);

            var addBtnRow = new VisualElement();
            addBtnRow.AddToClassList("add-btn-row");
            addBtnRow.Add(CreateFlexibleSpace());

            var addBtn = new Button(() =>
            {
                _valueFields.Add(new ValueField { fieldName = $"field{_valueFields.Count}", fieldType = "int" });
                _selectedFieldIndex = _valueFields.Count - 1;
                _fieldListView.RefreshItems();
                UpdatePreview();
            })
            {
                text = "+ 添加字段"
            };
            addBtn.AddToClassList("add-field-btn");
            addBtnRow.Add(addBtn);

            addBtnRow.Add(CreateFlexibleSpace());
            section.Add(addBtnRow);

            return section;
        }

        private VisualElement CreateOutputConfigSection()
        {
            var section = CreateSection("资源设置");

            section.Add(CreateLabeledField("资源输出目录", out _assetPathField, null));
            section.Add(CreateToggleField("打开生成的脚本", out _openScriptToggle));
            section.Add(CreateToggleField("自动创建资产", out _autoAssetToggle));

            return section;
        }

        private VisualElement CreatePreviewSection()
        {
            var section = CreateSection("代码预览");

            var previewLabel1 = new Label("配置表类:");
            previewLabel1.AddToClassList("preview-sub-label");
            section.Add(previewLabel1);

            _configPreviewField = new TextField("") { isReadOnly = true, multiline = true };
            _configPreviewField.AddToClassList("preview-text");
            _configPreviewField.style.height = 80;
            section.Add(_configPreviewField);

            var previewLabel2 = new Label("数据类:");
            previewLabel2.AddToClassList("preview-sub-label");
            section.Add(previewLabel2);

            _dataPreviewField = new TextField("") { isReadOnly = true, multiline = true };
            _dataPreviewField.AddToClassList("preview-text");
            _dataPreviewField.style.height = 120;
            section.Add(_dataPreviewField);

            return section;
        }

        private VisualElement CreateButtonSection()
        {
            var container = new VisualElement();
            container.AddToClassList("button-section");

            var btnRow = new VisualElement();
            btnRow.AddToClassList("btn-row");
            btnRow.Add(CreateFlexibleSpace());

            _generateCodeBtn = new Button(OnGenerateCodeOnlyClicked) { text = "仅生成代码" };
            _generateCodeBtn.AddToClassList("action-button");
            btnRow.Add(_generateCodeBtn);

            _generateAllBtn = new Button(OnGenerateAllClicked)
            {
                text = "生成代码并创建资产",
                name = "primary-action"
            };
            _generateAllBtn.AddToClassList("action-button");
            _generateAllBtn.AddToClassList("primary");
            btnRow.Add(_generateAllBtn);

            btnRow.Add(CreateFlexibleSpace());
            container.Add(btnRow);

            return container;
        }

        #endregion

        #region UI 工具方法

        private static VisualElement CreateFlexibleSpace()
        {
            var space = new VisualElement();
            space.style.flexGrow = 1;
            return space;
        }

        private static VisualElement CreateSection(string title)
        {
            var container = new VisualElement();
            container.AddToClassList("section");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("section-label");
            container.Add(titleLabel);
            return container;
        }

        private static VisualElement CreateLabeledField(string labelText, out TextField field,
            EventCallback<ChangeEvent<string>> onChange = null)
        {
            var row = new VisualElement();
            row.AddToClassList("field-row");
            var label = new Label(labelText);
            label.AddToClassList("field-label");
            row.Add(label);

            var fieldContainer = new VisualElement { name = "field-container" };
            fieldContainer.style.flexGrow = 1;

            field = new TextField("");
            field.AddToClassList("text-field");
            if (onChange != null) field.RegisterValueChangedCallback(onChange);
            fieldContainer.Add(field);
            row.Add(fieldContainer);

            return row;
        }

        private static VisualElement CreateIndentedField(string labelText, out TextField field)
        {
            var row = new VisualElement();
            row.AddToClassList("indented-row");
            var label = new Label(labelText);
            label.AddToClassList("indent-label");
            row.Add(label);

            field = new TextField("");
            field.AddToClassList("indent-field");
            row.Add(field);

            return row;
        }

        private static Toggle CreateToggleField(string labelText, out Toggle toggle)
        {
            toggle = new Toggle(labelText);
            toggle.AddToClassList("toggle-field");
            return toggle;
        }

        #endregion

        #region 数据绑定

        private void BindInitialValues()
        {
            _configNameField.value = _configName;
            _configNamespaceField.value = _configNamespace;
            _configOutputField.value = _configOutputPath;
            _dataNamespaceField.value = _dataNamespace;
            _dataOutputField.value = _dataOutputPath;
            _assetPathField.value = _outputAssetPath;
            _openScriptToggle.value = _openGeneratedScript;
            _autoAssetToggle.value = _autoCreateAsset;

            var keyIdx = Array.IndexOf(KeyTypeOptions, _keyType);
            if (keyIdx >= 0) _keyTypePopup.index = keyIdx;

            _valueTypeNameField.value = _valueTypeName;

            _fieldListView.itemsSource = _valueFields;
            _fieldListView.RefreshItems();
        }

        private void UpdatePreview()
        {
            if (_configNameField == null) return;

            _configName = _configNameField.value;
            _valueTypeName = _valueTypeNameField?.value ?? _valueTypeName;

            if (_configPreviewField != null)
                _configPreviewField.value = GenerateConfigClassCode();

            if (_dataPreviewField != null)
                _dataPreviewField.value = GenerateDataClassCode();

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var canGenerate = !string.IsNullOrEmpty(_configName) &&
                             !string.IsNullOrEmpty(_valueTypeName) &&
                             _valueFields.Count > 0;

            if (_generateCodeBtn != null) _generateCodeBtn.SetEnabled(canGenerate);
            if (_generateAllBtn != null) _generateAllBtn.SetEnabled(canGenerate);
        }

        #endregion

        #region 事件回调

        private void OnConfigNameChanged(ChangeEvent<string> evt)
        {
            _configName = evt.newValue;
            if (!string.IsNullOrEmpty(_configName) && _configName.EndsWith("Config"))
            {
                _valueTypeName = _configName.Substring(0, _configName.Length - "Config".Length) + "Data";
                _valueTypeNameField.value = _valueTypeName;
            }

            UpdatePreview();
        }

        private void OnGenerateCodeOnlyClicked()
        {
            try
            {
                SavePreferences();
                GenerateScriptFiles();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("成功",
                    "代码生成成功！\n\n" +
                    $"配置表路径：{_configOutputField.value}/{_configName}.cs\n" +
                    $"数据类路径：{_dataOutputField.value}/{_valueTypeName}.cs",
                    "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"生成失败：{ex.Message}", "确定");
                Debug.LogError($"[ConfigCreator] 生成失败: {ex}");
            }
        }

        private void OnGenerateAllClicked()
        {
            try
            {
                SavePreferences();
                GenerateScriptFiles();

                if (_autoAssetToggle.value)
                {
                    CreateConfigAsset();
                    EditorUtility.DisplayDialog("代码生成成功",
                        "配置表代码已生成！\n\n" +
                        $"配置表路径：{_configOutputField.value}/{_configName}.cs\n" +
                        $"数据类路径：{_dataOutputField.value}/{_valueTypeName}.cs\n\n" +
                        "资产将在编译完成后自动创建于：\n" +
                        $"{_assetPathField.value}/{_configName}.asset",
                        "确定");
                }
                else
                {
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("成功",
                        "配置表创建成功！\n\n" +
                        $"配置表路径：{_configOutputField.value}/{_configName}.cs\n" +
                        $"数据类路径：{_dataOutputField.value}/{_valueTypeName}.cs\n\n" +
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

        #endregion

        #region 生成逻辑

        private void GenerateScriptFiles()
        {
            var cfgOut = _configOutputField.value;
            var dataOut = _dataOutputField.value;

            if (!Directory.Exists(cfgOut)) Directory.CreateDirectory(cfgOut);
            if (!Directory.Exists(dataOut)) Directory.CreateDirectory(dataOut);

            var dataCode = GenerateDataClassCode();
            var dataFilePath = Path.Combine(dataOut, $"{_valueTypeName}.cs");
            File.WriteAllText(dataFilePath, dataCode, Encoding.UTF8);

            var configCode = GenerateConfigClassCode();
            var configFilePath = Path.Combine(cfgOut, $"{_configName}.cs");
            File.WriteAllText(configFilePath, configCode, Encoding.UTF8);

            Debug.Log($"[ConfigCreator] 生成文件：\n{dataFilePath}\n{configFilePath}");

            if (_openScriptToggle.value)
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
            var assetPath = _assetPathField.value;
            if (!Directory.Exists(assetPath)) Directory.CreateDirectory(assetPath);

            ConfigAssetCreator.RegisterPendingAsset(_configName, _configNamespaceField.value, assetPath);
            AssetDatabase.Refresh();
            Debug.Log("[ConfigCreator] 脚本已生成，等待编译完成后自动创建资产...");
        }

        private string GenerateDataClassCode()
        {
            var sb = new StringBuilder();
            var keyField = _valueFields.Find(f => f.isKeyField);
            if (keyField == null && _valueFields.Count > 0) keyField = _valueFields[0];

            sb.AppendLine("using System;");
            sb.AppendLine("using CFramework;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(_dataNamespace))
            {
                sb.AppendLine($"namespace {_dataNamespaceField.value}");
                sb.AppendLine("{");
            }

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {_valueTypeName} 数据结构");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public sealed class {_valueTypeName} : IConfigItem<{_keyType}>");
            sb.AppendLine("    {");

            foreach (var field in _valueFields)
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
                sb.AppendLine($"        public {_keyType} Key => {keyField.fieldName};");
                sb.AppendLine();
            }

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 克隆当前对象");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {_valueTypeName} Clone()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new {_valueTypeName}");
            sb.AppendLine("            {");

            for (var i = 0; i < _valueFields.Count; i++)
            {
                var field = _valueFields[i];
                sb.Append($"                {field.fieldName} = {field.fieldName}");
                sb.AppendLine(i < _valueFields.Count - 1 ? "," : "");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(_dataNamespace)) sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateConfigClassCode()
        {
            var sb = new StringBuilder();

            sb.AppendLine("using CFramework;");
            sb.AppendLine("using UnityEngine;");

            if (!string.IsNullOrEmpty(_dataNamespace) && _dataNamespace != _configNamespaceField.value)
                sb.AppendLine($"using {_dataNamespace};");

            sb.AppendLine();

            if (!string.IsNullOrEmpty(_configNamespaceField.value))
            {
                sb.AppendLine($"namespace {_configNamespaceField.value}");
                sb.AppendLine("{");
            }

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {_configName} 配置表");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    [CreateAssetMenu(fileName = \"{_configName}\", menuName = \"Game/Config/{_configName}\")]");
            sb.AppendLine(
                $"    public sealed class {_configName} : ConfigTableAsset<{_keyType}, {_valueTypeName}>");
            sb.AppendLine("    {");
            sb.AppendLine("        // 数据在 Inspector 中配置");
            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(_configNamespaceField.value)) sb.AppendLine("}");

            return sb.ToString();
        }

        private static bool IsNumericType(string type)
        {
            return type == "int" || type == "float" || type == "long" ||
                   type == "double" || type == "byte" || type == "short" ||
                   type == "uint" || type == "ulong" || type == "ushort";
        }

        #endregion

        #region 嵌套类型

        /// <summary>
        ///     字段列表项自定义元素
        /// </summary>
        private class FieldItemElement : VisualElement
        {
            private readonly Toggle _keyToggle;
            private readonly TextField _nameField;
            private readonly PopupField<string> _typePopup;
            private readonly Button _removeBtn;
            private readonly TextField _descField;
            private Action _onRemove;
            private Action _onSelect;
            private Action _onChanged;

            public FieldItemElement(string[] typeOptions)
            {
                AddToClassList("field-item");

                var topRow = new VisualElement();
                topRow.AddToClassList("item-top-row");

                _keyToggle = new Toggle("主键");
                _keyToggle.AddToClassList("key-toggle");
                _keyToggle.RegisterValueChangedCallback(_ => { _onChanged?.Invoke(); });
                topRow.Add(_keyToggle);

                _nameField = new TextField("");
                _nameField.AddToClassList("name-field");
                _nameField.RegisterValueChangedCallback(_ => { _onChanged?.Invoke(); });
                topRow.Add(_nameField);

                _typePopup = new PopupField<string>(new List<string>(typeOptions), 0);
                _typePopup.AddToClassList("type-popup");
                _typePopup.RegisterValueChangedCallback(_ => { _onChanged?.Invoke(); });
                topRow.Add(_typePopup);

                _removeBtn = new Button(() => _onRemove?.Invoke()) { text = "\u00D7" };
                _removeBtn.AddToClassList("remove-btn");
                topRow.Add(_removeBtn);

                Add(topRow);

                _descField = new TextField("描述");
                _descField.AddToClassList("desc-field");
                _descField.RegisterValueChangedCallback(_ => { _onChanged?.Invoke(); });
                Add(_descField);

                RegisterCallback<ClickEvent>(_ => _onSelect?.Invoke());
            }

            public void SetData(ValueField field, bool isSelected,
                Action onRemove, Action onSelect, Action onChanged)
            {
                _onRemove = onRemove;
                _onSelect = onSelect;
                _onChanged = onChanged;

                _keyToggle.value = field.isKeyField;
                _nameField.value = field.fieldName;
                _typePopup.value = field.fieldType;
                _descField.value = field.description ?? "";

                EnableInClassList("field-item-selected", isSelected);
            }
        }

        #endregion
    }

    /// <summary>
    ///     值类型字段定义（共享数据结构）
    /// </summary>
    [Serializable]
    public sealed class ValueField
    {
        public string fieldName;
        public string fieldType = "int";
        public bool isKeyField;
        public string description;
    }
}
