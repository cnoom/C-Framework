#if !ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CNoom.UnityTool.Editor;
using CFramework.Editor.Generators;
using CFramework.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows.Config
{
    /// <summary>
    ///     配置创建器窗口（UIToolkit 默认实现，不依赖 Odin）
    /// </summary>
    public sealed class ConfigCreatorWindow : EditorWindow
    {
        #region 菜单项

                /// <summary>
        ///     打开窗口（由 Dashboard 调用）
        /// </summary>
        public static void OpenWindow()
        {
            var window = GetWindow<ConfigCreatorWindow>("创建配置表");
            window.minSize = new Vector2(500, 620);
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

        // UIToolkit 控件引用
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

        // 编辑状态
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

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 加载 USS 样式表
            var styleSheet = EditorStyleSheet.Find("ConfigCreatorWindow.uss");
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            // 主滚动容器
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.AddToClassList("main-scroll");
            root.Add(scrollView);

            // 各区域
            scrollView.Add(CreateBasicConfigSection());
            scrollView.Add(CreateTypeConfigSection());
            scrollView.Add(CreateFieldListSection());
            scrollView.Add(CreateOutputConfigSection());
            scrollView.Add(CreatePreviewSection());
            scrollView.Add(CreateButtonSection());

            // 延迟绑定值
            EditorApplication.delayCall += () =>
            {
                BindInitialValues();
                UpdatePreview();
            };
        }

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
            if (_configNamespaceField != null)
            {
                EditorPrefs.SetString(PREF_CONFIG_NAMESPACE, _configNamespaceField.value);
                EditorPrefs.SetString(PREF_CONFIG_OUTPUT_PATH, _configOutputField.value);
                EditorPrefs.SetString(PREF_DATA_NAMESPACE, _dataNamespaceField.value);
                EditorPrefs.SetString(PREF_DATA_OUTPUT_PATH, _dataOutputField.value);
                EditorPrefs.SetString(PREF_ASSET_OUTPUT_PATH, _assetPathField.value);
                EditorPrefs.SetString(PREF_KEY_TYPE, _keyTypePopup.value);
            }
        }

        /// <summary>
        ///     绑定初始值到控件
        /// </summary>
        private void BindInitialValues()
        {
            _configNameField.value = configName;
            _configNamespaceField.value = configNamespace;
            _configOutputField.value = configOutputPath;
            _dataNamespaceField.value = dataNamespace;
            _dataOutputField.value = dataOutputPath;
            _assetPathField.value = outputAssetPath;
            _openScriptToggle.value = openGeneratedScript;
            _autoAssetToggle.value = autoCreateAsset;

            var keyIdx = Array.IndexOf(KeyTypeOptions, keyType);
            if (keyIdx >= 0) _keyTypePopup.index = keyIdx;

            _valueTypeNameField.value = valueTypeName;

            _fieldListView.itemsSource = valueFields;
            _fieldListView.RefreshItems();
        }

        #endregion

        #region UI 构建方法

        /// <summary>
        ///     创建基础配置区域
        /// </summary>
        private VisualElement CreateBasicConfigSection()
        {
            var section = CreateSection("基础配置");

            // 配置表名称
            section.Add(CreateLabeledField("配置表名称",
                out _configNameField,
                OnConfigNameChanged));

            // 配置表设置子区域
            var subLabel1 = new Label("配置表设置");
            subLabel1.AddToClassList("sub-label");
            section.Add(subLabel1);
            section.Add(CreateIndentedField("命名空间", out _configNamespaceField));
            section.Add(CreateIndentedField("输出目录", out _configOutputField));

            // 数据类设置子区域
            var subLabel2 = new Label("数据类设置");
            subLabel2.AddToClassList("sub-label");
            section.Add(subLabel2);
            section.Add(CreateIndentedField("命名空间", out _dataNamespaceField));
            section.Add(CreateIndentedField("输出目录", out _dataOutputField));

            return section;
        }

        /// <summary>
        ///     创建类型配置区域
        /// </summary>
        private VisualElement CreateTypeConfigSection()
        {
            var section = CreateSection("类型配置");

            // 键类型下拉框
            var keyRow = CreateLabeledField("键类型", out _, null);
            var keyPopupContainer = keyRow.Q<VisualElement>("field-container");

            _keyTypePopup = new PopupField<string>("", new List<string>(KeyTypeOptions), 0);
            _keyTypePopup.AddToClassList("popup-field");
            _keyTypePopup.RegisterValueChangedCallback(evt => { keyType = evt.newValue; });
            keyPopupContainer?.Clear();
            keyPopupContainer?.Add(_keyTypePopup);

            section.Add(keyRow);

            // 值类型名称
            section.Add(CreateLabeledField("值类型名称",
                out _valueTypeNameField,
                evt => { valueTypeName = evt.newValue; }));

            return section;
        }

        /// <summary>
        ///     创建字段列表区域
        /// </summary>
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
                        itemElem.SetData(valueFields[index], index == _selectedFieldIndex,
                            onRemove: () =>
                            {
                                valueFields.RemoveAt(index);
                                if (_selectedFieldIndex >= valueFields.Count)
                                    _selectedFieldIndex = valueFields.Count - 1;
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
                itemsSource = valueFields,
                selectionType = SelectionType.None,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                fixedItemHeight = 56,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };
            _fieldListView.AddToClassList("field-list");

            section.Add(_fieldListView);

            // 添加按钮行
            var addBtnRow = new VisualElement();
            addBtnRow.AddToClassList("add-btn-row");
            addBtnRow.Add(CreateFlexibleSpace());

            var addBtn = new Button(() =>
            {
                valueFields.Add(new ValueField { fieldName = $"field{valueFields.Count}", fieldType = "int" });
                _selectedFieldIndex = valueFields.Count - 1;
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

        /// <summary>
        ///     创建资源输出配置区域
        /// </summary>
        private VisualElement CreateOutputConfigSection()
        {
            var section = CreateSection("资源设置");

            section.Add(CreateLabeledField("资源输出目录", out _assetPathField, null));
            section.Add(CreateToggleField("打开生成的脚本", out _openScriptToggle));
            section.Add(CreateToggleField("自动创建资产", out _autoAssetToggle));

            return section;
        }

        /// <summary>
        ///     创建代码预览区域
        /// </summary>
        private VisualElement CreatePreviewSection()
        {
            var section = CreateSection("代码预览");

            var previewLabel1 = new Label("配置表类:");
            previewLabel1.AddToClassList("preview-sub-label");
            section.Add(previewLabel1);

            _configPreviewField = new TextField("")
            {
                isReadOnly = true,
                multiline = true
            };
            _configPreviewField.AddToClassList("preview-text");
            _configPreviewField.style.height = 80;
            section.Add(_configPreviewField);

            var previewLabel2 = new Label("数据类:");
            previewLabel2.AddToClassList("preview-sub-label");
            section.Add(previewLabel2);

            _dataPreviewField = new TextField("")
            {
                isReadOnly = true,
                multiline = true
            };
            _dataPreviewField.AddToClassList("preview-text");
            _dataPreviewField.style.height = 120;
            section.Add(_dataPreviewField);

            return section;
        }

        /// <summary>
        ///     创建操作按钮区域
        /// </summary>
        private VisualElement CreateButtonSection()
        {
            var container = new VisualElement();
            container.AddToClassList("button-section");

            var btnRow = new VisualElement();
            btnRow.AddToClassList("btn-row");
            btnRow.Add(CreateFlexibleSpace());

            _generateCodeBtn = new Button(OnGenerateCodeOnlyClicked)
            {
                text = "仅生成代码"
            };
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

        /// <summary>
        ///     创建弹性空白区域
        /// </summary>
        private static VisualElement CreateFlexibleSpace()
        {
            var space = new VisualElement();
            space.style.flexGrow = 1;
            return space;
        }

        /// <summary>
        ///     创建分区容器
        /// </summary>
        private static VisualElement CreateSection(string title)
        {
            var container = new VisualElement();
            container.AddToClassList("section");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("section-label");
            container.Add(titleLabel);
            return container;
        }

        /// <summary>
        ///     创建带标签的文本字段行
        /// </summary>
        private static VisualElement CreateLabeledField(string labelText, out TextField field, EventCallback<ChangeEvent<string>> onChange = null)
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

        /// <summary>
        ///     创建缩进文本字段行
        /// </summary>
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

        /// <summary>
        ///     创建开关行
        /// </summary>
        private static Toggle CreateToggleField(string labelText, out Toggle toggle)
        {
            toggle = new Toggle(labelText);
            toggle.AddToClassList("toggle-field");
            return toggle;
        }

        /// <summary>
        ///     更新代码预览
        /// </summary>
        private void UpdatePreview()
        {
            if (_configNameField == null) return;

            configName = _configNameField.value;
            valueTypeName = _valueTypeNameField?.value ?? valueTypeName;

            if (_configPreviewField != null)
                _configPreviewField.value = GenerateConfigClassCode();

            if (_dataPreviewField != null)
                _dataPreviewField.value = GenerateDataClassCode();

            UpdateButtonStates();
        }

        /// <summary>
        ///     更新按钮可用状态
        /// </summary>
        private void UpdateButtonStates()
        {
            var canGenerate = !string.IsNullOrEmpty(configName) &&
                             !string.IsNullOrEmpty(valueTypeName) &&
                             valueFields.Count > 0;

            if (_generateCodeBtn != null) _generateCodeBtn.SetEnabled(canGenerate);
            if (_generateAllBtn != null) _generateAllBtn.SetEnabled(canGenerate);
        }

        #endregion

        #region 事件回调

        private void OnConfigNameChanged(ChangeEvent<string> evt)
        {
            configName = evt.newValue;
            if (!string.IsNullOrEmpty(configName) && configName.EndsWith("Config"))
            {
                valueTypeName = configName.Substring(0, configName.Length - "Config".Length) + "Data";
                _valueTypeNameField.value = valueTypeName;
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
                    $"配置表路径：{_configOutputField.value}/{configName}.cs\n" +
                    $"数据类路径：{_dataOutputField.value}/{valueTypeName}.cs",
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
                        $"配置表路径：{_configOutputField.value}/{configName}.cs\n" +
                        $"数据类路径：{_dataOutputField.value}/{valueTypeName}.cs\n\n" +
                        "资产将在编译完成后自动创建于：\n" +
                        $"{_assetPathField.value}/{configName}.asset",
                        "确定");
                }
                else
                {
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("成功",
                        "配置表创建成功！\n\n" +
                        $"配置表路径：{_configOutputField.value}/{configName}.cs\n" +
                        $"数据类路径：{_dataOutputField.value}/{valueTypeName}.cs\n\n" +
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

        private string GetCurrentDataNamespace() => _dataNamespaceField?.value ?? dataNamespace;

        private string GetCurrentConfigNamespace() => _configNamespaceField?.value ?? configNamespace;

        private void GenerateScriptFiles()
        {
            Generators.ConfigCodeGenerator.WriteScriptFiles(
                _configOutputField.value,
                _dataOutputField.value,
                configName,
                valueTypeName,
                GetCurrentConfigNamespace(),
                GetCurrentDataNamespace(),
                keyType,
                valueFields,
                _openScriptToggle.value);
        }

        private void CreateConfigAsset()
        {
            var assetPath = _assetPathField.value;
            if (!Directory.Exists(assetPath)) Directory.CreateDirectory(assetPath);

            ConfigAssetCreator.RegisterPendingAsset(configName, _configNamespaceField.value, assetPath);
            AssetDatabase.Refresh();
            Debug.Log("[ConfigCreator] 脚本已生成，等待编译完成后自动创建资产...");
        }

        private string GenerateDataClassCode()
        {
            return Generators.ConfigCodeGenerator.GenerateDataClassCode(
                valueTypeName, keyType, GetCurrentDataNamespace(), valueFields);
        }

        private string GenerateConfigClassCode()
        {
            return Generators.ConfigCodeGenerator.GenerateConfigClassCode(
                configName, GetCurrentConfigNamespace(), GetCurrentDataNamespace(),
                keyType, valueTypeName);
        }

        #endregion

        #endregion

        #region 字段列表项元素

        /// <summary>
        ///     字段列表项自定义元素
        /// </summary>
        private class FieldItemElement : VisualElement
        {
            private Toggle _keyToggle;
            private TextField _nameField;
            private PopupField<string> _typePopup;
            private Button _removeBtn;
            private TextField _descField;
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
                _keyToggle.RegisterValueChangedCallback(evt => { _onChanged?.Invoke(); });
                topRow.Add(_keyToggle);

                _nameField = new TextField("");
                _nameField.AddToClassList("name-field");
                _nameField.RegisterValueChangedCallback(evt => { _onChanged?.Invoke(); });
                topRow.Add(_nameField);

                _typePopup = new PopupField<string>(new List<string>(typeOptions), 0);
                _typePopup.AddToClassList("type-popup");
                _typePopup.RegisterValueChangedCallback(evt => { _onChanged?.Invoke(); });
                topRow.Add(_typePopup);

                _removeBtn = new Button(() => _onRemove?.Invoke())
                {
                    text = "\u00D7"  // × 符号
                };
                _removeBtn.AddToClassList("remove-btn");
                topRow.Add(_removeBtn);

                Add(topRow);

                _descField = new TextField("描述");
                _descField.AddToClassList("desc-field");
                _descField.RegisterValueChangedCallback(evt => { _onChanged?.Invoke(); });
                Add(_descField);

                // 点击选中
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
}
#endif
