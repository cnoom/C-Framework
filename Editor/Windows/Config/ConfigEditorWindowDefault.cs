#if !ODIN_INSPECTOR
using CFramework;
using System;
using System.Collections.Generic;
using CNoom.UnityTool.Editor;
using UnityEditor;
using UnityEditor.UIElements;
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

        [MenuItem("CFramework/配置管理", priority = 100)]
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

        #endregion

        #region 数据字段

        private readonly List<ConfigInfo> _configs = new();
        private int _selectedIndex = -1;

        private UnityEditor.Editor _configEditor;
        private SerializedObject _serializedConfig;
        private ConfigTableAsset _selectedConfig;

        #endregion

        #region 生命周期

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 加载 USS 样式表
            var styleSheet = EditorStyleSheet.Find("ConfigEditorWindow.uss");
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            // ===== 工具栏 =====
            _toolbar = new Toolbar();
            _toolbar.AddToClassList("main-toolbar");

            var newConfigBtn = new Button(() => ConfigCreatorWindow.OpenWindow())
            {
                text = "新建配置"
            };
            newConfigBtn.AddToClassList("toolbar-btn");
            _toolbar.Add(newConfigBtn);

            var refreshBtn = new Button(RefreshConfigList)
            {
                text = "刷新"
            };
            refreshBtn.AddToClassList("toolbar-btn");
            _toolbar.Add(refreshBtn);

            _toolbar.Add(CreateFlexibleSpace());

            _countLabel = new Label("共 0 个配置表");
            _countLabel.AddToClassList("count-label");
            _toolbar.Add(_countLabel);

            root.Add(_toolbar);

            // ===== 主分屏视图 =====
            _splitView = new TwoPaneSplitView(0, 260, TwoPaneSplitViewOrientation.Horizontal);
            _splitView.AddToClassList("split-view");
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
        ///     创建弹性空白区域
        /// </summary>
        private static VisualElement CreateFlexibleSpace()
        {
            var space = new VisualElement();
            space.style.flexGrow = 1;
            return space;
        }

        /// <summary>
        ///     创建左侧配置列表面板
        /// </summary>
        private VisualElement CreateLeftPanel()
        {
            var leftPane = new VisualElement();
            leftPane.AddToClassList("left-pane");

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
            _configListView.selectionChanged += objects =>
            {
                foreach (var obj in objects)
                {
                    if (obj is int idx)
                    {
                        SelectConfig(idx);
                    }
                    break;
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
            var rightPane = new VisualElement();
            rightPane.AddToClassList("right-pane");

            // 详情标题
            _detailTitleLabel = new Label("");
            _detailTitleLabel.AddToClassList("detail-title");
            rightPane.Add(_detailTitleLabel);

            // 路径信息
            var pathRow = new VisualElement();
            pathRow.AddToClassList("path-row");
            var pathPrefix = new Label("路径:");
            pathPrefix.AddToClassList("path-prefix");
            pathRow.Add(pathPrefix);
            _pathLabel = new Label("");
            _pathLabel.AddToClassList("path-value");
            pathRow.Add(_pathLabel);
            rightPane.Add(pathRow);

            // 分割线
            _divider = new VisualElement();
            _divider.AddToClassList("detail-divider");
            rightPane.Add(_divider);

            // 内容滚动区
            _rightScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            _rightScrollView.AddToClassList("content-scroll");
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

            var emptyContainer = new VisualElement();
            emptyContainer.AddToClassList("empty-container");

            var message = _configs.Count == 0
                ? "暂无配置表\n请点击「新建配置」按钮创建"
                : "请在左侧选择一个配置表";

            var messageLabel = new Label(message);
            messageLabel.AddToClassList("empty-message");
            emptyContainer.Add(messageLabel);

            _rightScrollView.Add(emptyContainer);
        }

        /// <summary>
        ///     显示配置详情
        /// </summary>
        private void ShowConfigDetail()
        {
            _rightScrollView.Clear();

            if (_serializedConfig != null)
            {
                // 使用 InspectorElement 直接展示序列化对象
                var inspector = new InspectorElement(_serializedConfig);
                _rightScrollView.Add(inspector);
            }
            else
            {
                _rightScrollView.Add(CreateDefaultInspectorFallback());
            }
        }

        /// <summary>
        ///     创建备用 Inspector 视图
        /// </summary>
        private VisualElement CreateDefaultInspectorFallback()
        {
            if (_configEditor == null) return new Label("无法加载编辑器");

            var container = new VisualElement();
            var inspector = new InspectorElement(_configEditor.serializedObject);
            container.Add(inspector);
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
            _selectedConfig = config.Asset as ConfigTableAsset;

            if (_selectedConfig == null) return;

            _serializedConfig = new SerializedObject(_selectedConfig);
            _configEditor = UnityEditor.Editor.CreateEditor(_selectedConfig);

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

            // 按 ConfigTableAsset 子类类型精准搜索
            var configTypes = TypeCache.GetTypesDerivedFrom<ConfigTableAsset>();
            var visitedPaths = new HashSet<string>();

            foreach (var type in configTypes)
            {
                var guids = AssetDatabase.FindAssets($"t:{type.Name}");

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!visitedPaths.Add(path)) continue;

                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset is ConfigTableAsset configTable)
                    {
                        var configType = configTable.GetType();
                        _configs.Add(new ConfigInfo
                        {
                            Name = configType.Name,
                            Type = configType.BaseType?.Name ?? "ConfigTableAsset",
                            Count = configTable.Count,
                            Path = path,
                            Asset = asset,
                            ConfigType = configType
                        });
                    }
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

                _nameLabel = new Label();
                _nameLabel.AddToClassList("config-name");
                Add(_nameLabel);

                var metaRow = new VisualElement();
                metaRow.AddToClassList("config-meta-row");
                _typeLabel = new Label();
                _typeLabel.AddToClassList("config-type");
                metaRow.Add(_typeLabel);
                _countLabel = new Label();
                _countLabel.AddToClassList("config-count");
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
