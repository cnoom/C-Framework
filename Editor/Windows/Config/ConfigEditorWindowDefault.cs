#if !ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows.Config
{
    /// <summary>
    ///     配置管理编辑器窗口（UIToolkit 默认实现，不依赖 Odin）
    /// </summary>
    public sealed class ConfigEditorWindow : EditorWindow
    {
        #region 菜单项

        [MenuItem("CFramework/配置管理", false, 100)]
        public static void OpenWindow()
        {
            var window = GetWindow<ConfigEditorWindow>("配置管理");
            window.minSize = new Vector2(900, 500);
            window.Show();
            window.RefreshConfigList();
        }

        #endregion

        #region 数据结构

        /// <summary>
        ///     配置信息
        /// </summary>
        [Serializable]
        public sealed class ConfigInfo
        {
            public string Name;
            public string Type;
            public int Count;
            public string Path;
            public ScriptableObject Asset;
            public Type ConfigType;

            public override string ToString()
            {
                return Name;
            }
        }

        #endregion

        #region UIToolkit 控件

        private Toolbar _toolbar;
        private ToolbarSearchField _searchField;
        private Label _countLabel;

        private TwoPaneSplitView _splitView;

        private ListView _configListView;
        private ScrollView _rightScrollView;

        // 右侧面板内容
        private Label _detailTitleLabel;
        private Label _pathLabel;
        private VisualElement _divider;

        // ReorderableList 替代品
        private ListView _dataListView;
        private PropertyField _defaultInspector;

        #endregion

        #region 数据字段

        private List<ConfigInfo> _configs = new();
        private int _selectedIndex = -1;

        private UnityEditor.Editor _configEditor;
        private SerializedObject _serializedConfig;
        private ConfigTableBase _selectedConfig;

        #endregion

        #region 生命周期

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 样式
            root.styleSheets.Add(CreateStyleSheet());

            // ===== 工具栏 =====
            _toolbar = new Toolbar { AddToClassList("main-toolbar") };

            var newConfigBtn = new Button(() => ConfigCreatorWindow.OpenWindow())
            {
                text = "新建配置",
                AddToClassList("toolbar-btn"
            };
            _toolbar.Add(newConfigBtn);

            var refreshBtn = new Button(RefreshConfigList)
            {
                text = "刷新",
                AddToClassList("toolbar-btn"
            };
            _toolbar.Add(refreshBtn);

            _toolbar.Add(new FlexibleSpace());

            _countLabel = new Label("共 0 个配置表") { AddToClassList("count-label") };
            _toolbar.Add(_countLabel);

            root.Add(_toolbar);

            // ===== 主分屏视图 =====
            _splitView = new TwoPaneSplitView(0, 260, TwoPaneSplitViewResizeMode.Flexible)
            {
                AddToClassList("split-view"
            };
            root.Add(_splitView);

            // 左侧面板：配置列表
            var leftPane = CreateLeftPanel();
            _splitView.Add(leftPane);

            // 右侧面板：详情
            var rightPane = CreateRightPanel();
            _splitView.Add(rightPane);
        }

        private void OnDisable()
        {
            CleanupEditor();
        }

        private void OnDestroy()
        {
            CleanupEditor();
        }

        #endregion

        #region UI 构建

        /// <summary>
        ///     创建左侧配置列表面板
        /// </summary>
        private VisualElement CreateLeftPanel()
        {
            var leftPane = new VisualElement { AddToClassList("left-pane") };

            // 搜索栏
            _searchField = new ToolbarSearchField { name = "config-search" };
            _searchField.RegisterValueChangedCallback(evt =>
            {
                ApplySearchFilter(evt.newValue);
            });
            leftPane.Add(_searchField);

            // 配置列表
            _configListView = new ListView
            {
                makeItem = () => new ConfigListItemElement(),
                bindItem = (element, index) =>
                {
                    if (element is ConfigListItemElement itemElem)
                    {
                        itemElem.SetData(
                            _configs[index],
                            index == _selectedIndex,
                            OnConfigSelected
                        );
                    }
                },
                itemsSource = _configs,
                selectionType = SelectionType.Single,
                showBorder = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                fixedItemHeight = 48,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };
            _configListView.AddToClassList("config-list");
            _configListView.selectionChanged += indices =>
            {
                if (indices.Count > 0)
                {
                    SelectConfig(indices[0]);
                }
            };

            leftPane.Add(_configListView);
            return leftPane;
        }

        /// <summary>
        ///     创建右侧详情面板
        /// </summary>
        private VisualElement CreateRightPanel()
        {
            var rightPane = new VisualElement { AddToClassList("right-pane") };

            // 详情标题
            _detailTitleLabel = new Label("") { AddToClassList("detail-title") };
            rightPane.Add(_detailTitleLabel);

            // 路径信息
            var pathRow = new VisualElement { AddToClassList("path-row") };
            pathRow.Add(new Label("路径:") { AddToClassList("path-prefix") });
            _pathLabel = new Label("") { AddToClassList("path-value") };
            pathRow.Add(_pathLabel);
            rightPane.Add(pathRow);

            // 分割线
            _divider = new VisualElement { AddToClassList("detail-divider") };
            rightPane.Add(_divider);

            // 内容滚动区
            _rightScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
            {
                AddToClassList("content-scroll"
            };
            rightPane.Add(_rightScrollView);

            // 空状态提示
            ShowEmptyState();

            return rightPane;
        }

        /// <summary>
        ///     显示空状态提示
        /// </summary>
        private void ShowEmptyState()
        {
            _rightScrollView.Clear();

            var emptyContainer = new VisualElement { AddToClassList("empty-container") };

            var message = _configs.Count == 0
                ? "暂无配置表\n请点击「新建配置」按钮创建"
                : "请在左侧选择一个配置表";

            var messageLabel = new Label(message) { AddToClassList("empty-message") };
            emptyContainer.Add(messageLabel);

            _rightScrollView.Add(emptyContainer);
        }

        /// <summary>
        ///     显示配置详情
        /// </summary>
        private void ShowConfigDetail()
        {
            _rightScrollView.Clear();

            // 使用 PropertyField + SerializedObject 绘制 dataList 属性
            if (_serializedConfig != null)
            {
                _dataListView = new ListView
                {
                    makeItem = () => new PropertyField(null),
                    bindProperty = (element, prop) =>
                    {
                        ((PropertyField)element).bindingPath = "";
                        // 使用 InspectorElement 绑定序列化属性
                        element.Bind(_serializedContext);
                    },
                    itemsSource = null,
                    showBorder = true,
                    showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                    fixedItemHeight = 22,
                    reorderable = true,
                    reorderMode = ListViewReorderMode.Animated
                };

                // 尝试获取 dataList 属性并绑定到 ListView
                var dataListProp = _serializedConfig.FindProperty("dataList");
                if (dataListProp != null && dataListProp.isArray)
                {
                    // 创建属性数组包装器用于 ListView
                    _dataListView.itemSource = dataListProp.arraySize;
                    _dataListView.makeItem = () => new VisualElement { AddToClassList("data-item") };
                    _dataListView.bindItem = (element, idx) =>
                    {
                        element.Clear();
                        var elementProp = dataListProp.GetArrayElementAtIndex(idx);
                        DrawElementProperties(element, elementProp, idx);
                    };

                    _dataListView.AddItemHeight = CalculateElementHeight(dataListProp) + 8;
                }

                _dataListView.AddToClassList("data-list-view");
                _rightScrollView.Add(_dataListView);
            }
            else
            {
                // 备用方案：使用默认 Inspector
                _rightScrollView.Add(CreateDefaultInspectorFallback());
            }
        }

        /// <summary>
        ///     绘制单个数据元素的属性
        /// </summary>
        private void DrawElementProperties(VisualElement parent, SerializedProperty element, int index)
        {
            var child = element.Copy();
            var endProp = element.GetEndProperty();

            if (child.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(child, endProp))
                {
                    var field = new PropertyField(child);
                    field.label = child.displayName;
                    field.bindingPath = child.propertyPath;
                    parent.Add(field);

                    if (!child.NextVisible(false)) break;
                }
            }
        }

        /// <summary>
        ///     计算元素高度
        /// </summary>
        private float CalculateElementHeight(SerializedProperty element)
        {
            var child = element.Copy();
            var endProp = element.GetEndProperty();
            var height = EditorGUIUtility.singleLineHeight;

            if (child.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(child, endProp))
                {
                    height = Mathf.Max(height, EditorGUI.GetPropertyHeight(child));
                    if (!child.NextVisible(false)) break;
                }
            }

            return height;
        }

        /// <summary>
        ///     创建备用 Inspector 视图
        /// </summary>
        private VisualElement CreateDefaultInspectorFallback()
        {
            if (_configEditor == null) return new Label("无法加载编辑器");

            var container = new VisualElement();
            InspectorElement.CreateInspectorElement(container, _configEditor.serializedObject, _configEditor, false);
            return container;
        }

        #endregion

        #region 操作逻辑

        /// <summary>
        ///     应用搜索过滤
        /// </summary>
        private void ApplySearchFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                _configListView.itemsSource = _configs;
            }
            else
            {
                var filtered = _configs.FindAll(c =>
                    c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
                _configListView.itemsSource = filtered;
            }

            _configListView.RefreshItems();
        }

        /// <summary>
        ///     选择配置
        /// </summary>
        private void SelectConfig(int index)
        {
            if (_selectedIndex == index && _selectedConfig != null) return;

            _selectedIndex = index;
            CleanupEditor();

            if (index < 0 || index >= _configs.Count) return;

            var config = _configs[index];
            _selectedConfig = config.Asset as ConfigTableBase;

            if (_selectedConfig == null) return;

            _serializedConfig = new SerializedObject(_selectedConfig);

            // 更新右侧面板
            var configName = _selectedConfig.GetType().Name;
            _detailTitleLabel.text = _selectedIndex >= 0 && _selectedIndex < _configs.Count
                ? $"配置详情 - {configName} ({_configs[_selectedIndex].Count} 条记录)"
                : $"配置详情 - {configName}";

            _pathLabel.text = config.Path;
            _pathLabel.parent.style.display = !string.IsNullOrEmpty(config.Path)
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _divider.style.display = DisplayStyle.Flex;

            ShowConfigDetail();
            Repaint();
        }

        private void OnConfigSelected(ConfigInfo info)
        {
            var index = _configs.IndexOf(info);
            if (index >= 0)
            {
                SelectConfig(index);
                _configListView.selectedIndex = index;
            }
        }

        /// <summary>
        ///     刷新配置列表
        /// </summary>
        public void RefreshConfigList()
        {
            _configs.Clear();
            _selectedIndex = -1;
            _selectedConfig = null;
            CleanupEditor();

            var guids = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is ConfigTableBase configTable)
                {
                    var configType = configTable.GetType();
                    _configs.Add(new ConfigInfo
                    {
                        Name = configType.Name,
                        Type = configType.BaseType?.Name ?? "ConfigTableBase",
                        Count = configTable.Count,
                        Path = path,
                        Asset = asset,
                        ConfigType = configType
                    });
                }
            }

            _countLabel.text = $"共 {_configs.Count} 个配置表";

            _configListView.itemsSource = _configs;
            _configListView.RefreshItems();

            ShowEmptyState();

            Debug.Log($"[ConfigEditor] 找到 {_configs.Count} 个配置表");
        }

        private void CleanupEditor()
        {
            if (_configEditor != null)
            {
                DestroyImmediate(_configEditor);
                _configEditor = null;
            }

            _serializedConfig?.Dispose();
            _serializedConfig = null;
        }

        #endregion

        #region 样式

        private static StyleSheet CreateStyleSheet()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine(".main-toolbar {");
            sb.AppendLine("    background-color: rgb(55, 55, 58);");
            sb.AppendLine("}");

            sb.AppendLine(".toolbar-btn {");
            sb.AppendLine("-unity-font-style: normal;}");
            sb.AppendLine("}");

            sb.AppendLine(".count-label {");
            sb.AppendLine("    font-size: 11px;");
            sb.AppendLine("    color: rgb(150, 150, 150);");
            sb.AppendLine("    margin-right: 8px;");
            sb.AppendLine("}");

            sb.AppendLine(".split-view {");
            sb.AppendLine("    flex-grow: 1;");
            sb.AppendLine("}");

            sb.AppendLine(".left-pane {");
            sb.AppendLine("    flex-direction: column;");
            sb.AppendLine("    padding: 4px;");
            sb.AppendLine("    background-color: rgba(38, 38, 40, 0.95);");
            sb.AppendLine("}");

            sb.AppendLine("#config-search { margin-bottom: 4px; }");

            sb.AppendLine(".config-list {");
            sb.AppendLine("    flex-grow: 1;");
            sb.AppendLine("}");

            sb.AppendLine(".right-pane {");
            sb.AppendLine("    flex-direction: column;");
            sb.AppendLine("    padding: 8px;");
            sb.AppendLine("    background-color: rgba(32, 32, 35, 0.95);");
            sb.AppendLine("}");

            sb.AppendLine(".detail-title {");
            sb.AppendLine("    font-size: 13px;");
            sb.AppendLine("    font-weight: bold;");
            sb.AppendLine("    color: rgb(200, 200, 200);");
            sb.AppendLine("    margin-bottom: 4px;");
            sb.AppendLine("}");

            sb.AppendLine(".path-row {");
            sb.AppendLine("    flex-direction: row;");
            sb.AppendLine("    align-items: center;");
            sb.AppendLine("    margin-bottom: 6px;");
            sb.AppendLine("}");

            sb.AppendLine(".path-prefix {");
            sb.AppendLine("    font-size: 11px;");
            sb.AppendLine("    color: rgb(130, 130, 130);");
            sb.AppendLine("    margin-right: 4px;");
            sb.AppendLine("}");

            sb.AppendLine(".path-value {");
            sb.AppendLine("    font-size: 11px;");
            sb.AppendLine("    color: rgb(140, 140, 140);");
            sb.AppendLine("}");

            sb.AppendLine(".detail-divider {");
            sb.AppendLine("    height: 1px;");
            sb.AppendLine("    background-color: rgb(48, 48, 48);");
            sb.AppendLine("    margin-bottom: 8px;");
            sb.AppendLine("}");

            sb.AppendLine(".content-scroll {");
            sb.AppendLine("    flex-grow: 1;");
            sb.AppendLine("}");

            // 空状态样式
            sb.AppendLine(".empty-container {");
            sb.AppendLine("    flex-direction: row;");
            sb.AppendLine("    justify-content: center;");
            sb.AppendLine("    align-items: center;");
            sb.AppendLine("    height: 100%;");
            sb.AppendLine("}");

            sb.AppendLine(".empty-message {");
            sb.AppendLine("    font-size: 14px;");
            sb.AppendLine("    color: rgb(140, 140, 140);");
            sb.AppendLine("-unity-text-align: middle;}");
            sb.AppendLine("}");

            // 列表项样式
            sb.AppendLine(".config-item {");
            sb.AppendLine("    flex-direction: column;");
            sb.AppendLine("    padding: 6px 10px;");
            sb.AppendLine("    border-radius: 3px;");
            sb.AppendLine("    cursor: pointer;");
            sb.AppendLine("}");

            sb.AppendLine(".config-item:hover {");
            sb.AppendLine("    background-color: rgba(60, 90, 140, 0.15);");
            sb.AppendLine("}");

            sb.AppendLine(".config-item.selected {");
            sb.AppendLine("    background-color: rgba(60, 125, 190, 0.25);");
            sb.AppendLine("}");

            sb.AppendLine(".config-name {");
            sb.AppendLine("    font-size: 12px;");
            sb.AppendLine("    font-weight: bold;");
            sb.AppendLine("    color: rgb(210, 210, 210);");
            sb.AppendLine("}");

            sb.AppendLine(".config-meta-row {");
            sb.AppendLine("    flex-direction: row;");
            sb.AppendLine("    margin-top: 2px;");
            sb.AppendLine("}");

            sb.AppendLine(".config-type {");
            sb.AppendLine("    font-size: 10px;");
            sb.AppendLine("    color: rgb(130, 130, 130);");
            sb.AppendLine("}");

            sb.AppendLine(".config-count {");
            sb.AppendLine("    font-size: 10px;");
            sb.AppendLine("    color: rgb(130, 130, 130);");
            sb.AppendLine("    margin-left: 8px;");
            sb.AppendLine("}");

            // 数据列表视图
            sb.AppendLine(".data-list-view {");
            sb.AppendLine("    flex-grow: 1;");
            sb.AppendLine("}");

            var styleSheet = new StyleSheet();
            // 样式通过内联方式应用到各控件
            return styleSheet;
        }

        #endregion

        #region 配置列表项元素

        /// <summary>
        ///     配置列表项自定义元素
        /// </summary>
        private class ConfigListItemElement : VisualElement
        {
            private readonly Label _nameLabel;
            private readonly Label _typeLabel;
            private readonly Label _countLabel;
            private ConfigInfo _boundInfo;
            private Action<ConfigInfo> _onSelected;

            public ConfigListItemElement()
            {
                AddToClassList("config-item");

                _nameLabel = new Label() { AddToClassList("config-name") };
                Add(_nameLabel);

                var metaRow = new VisualElement { AddToClassList("config-meta-row") };
                _typeLabel = new Label() { AddToClassList("config-type") };
                metaRow.Add(_typeLabel);
                _countLabel = new Label() { AddToClassList("config-count") };
                metaRow.Add(_countLabel);
                Add(metaRow);

                // 点击事件
                RegisterCallback<ClickEvent>(_ =>
                {
                    _onSelected?.Invoke(_boundInfo);
                });
            }

            public void SetData(ConfigInfo info, bool isSelected, Action<ConfigInfo> onSelected)
            {
                _boundInfo = info;
                _onSelected = onSelected;

                _nameLabel.text = info.Name;
                _typeLabel.text = info.Type;
                _countLabel.text = $"{info.Count} 条";

                EnableInClassList("selected", isSelected);
            }
        }

        #endregion
    }
}
#endif
