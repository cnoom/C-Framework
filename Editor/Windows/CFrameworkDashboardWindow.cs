using System;
using System.Collections.Generic;
using CFramework;
using CNoom.UnityTool.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows
{
    /// <summary>
    ///     CFramework 统一编辑器面板
    ///     将配置管理、创建配置等编辑器工具整合为 Tab 界面
    /// </summary>
    public sealed class CFrameworkDashboardWindow : EditorWindow
    {
        #region 菜单项

        [MenuItem("CFramework/Dashboard", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<CFrameworkDashboardWindow>("CFramework Dashboard");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        #endregion

        #region Tab 定义

        private const int TAB_CONFIG_EDITOR = 0;
        private const int TAB_CONFIG_CREATOR = 1;
        private const int TAB_AUDIO_DEBUGGER = 2;
        private const int TAB_EXCEPTION_VIEWER = 3;

        private static readonly string[] TabNames = { "配置管理", "创建配置", "音频调试", "异常查看" };

        #endregion

        #region Tab 控件

        private readonly List<ToolbarToggle> _tabToggles = new();
        private VisualElement _contentContainer;
        private int _activeTabIndex = -1;

        #endregion

        #region 配置管理 Tab 字段

        private readonly List<ConfigInfo> _configs = new();
        private int _selectedIndex = -1;

        private ToolbarSearchField _searchField;
        private Label _countLabel;
        private ListView _configListView;
        private ScrollView _rightScrollView;
        private Label _detailTitleLabel;
        private Label _pathLabel;
        private VisualElement _divider;

        private UnityEditor.Editor _configEditor;
        private SerializedObject _serializedConfig;
        private ConfigTableAsset _selectedConfig;

        #endregion

        #region 配置创建 Tab

        private DashboardConfigCreatorTab _creatorTab;

        #endregion

        #region 音频调试 Tab

#if CFRAMEWORK_AUDIO
        private DashboardAudioDebuggerTab _audioDebuggerTab;
#endif
        private VisualElement _audioDebuggerRoot;

        #endregion

        #region 异常查看 Tab

        private DashboardExceptionViewerTab _exceptionViewerTab;
        private VisualElement _exceptionViewerRoot;

        #endregion

        #region 生命周期

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 加载 USS 样式表（Dashboard 自身 + 各子模块）
            LoadStyleSheets(root);

            // Tab 栏
            var tabBar = new Toolbar();
            tabBar.AddToClassList("tab-bar");

            for (var i = 0; i < TabNames.Length; i++)
            {
                var index = i;
                var toggle = new ToolbarToggle
                {
                    text = TabNames[i],
                    value = false
                };
                toggle.AddToClassList("tab-toggle");
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue) SwitchTab(index);
                });
                tabBar.Add(toggle);
                _tabToggles.Add(toggle);
            }

            tabBar.Add(new VisualElement { style = { flexGrow = 1 } });

            _countLabel = new Label("");
            _countLabel.AddToClassList("count-label");
            _countLabel.style.display = DisplayStyle.None;
            tabBar.Add(_countLabel);

            root.Add(tabBar);

            // 分割线
            var tabDivider = new VisualElement();
            tabDivider.AddToClassList("tab-divider");
            root.Add(tabDivider);

            // 内容容器
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList("tab-content");
            _contentContainer.style.flexGrow = 1;
            root.Add(_contentContainer);

            // 初始化各 Tab 内容
            InitConfigEditorContent();
            InitConfigCreatorContent();
            InitAudioDebuggerContent();
            InitExceptionViewerContent();

            // 注册 Play Mode 状态变化回调
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // 默认激活第一个 Tab
            SwitchTab(TAB_CONFIG_EDITOR);
        }

        private void OnDestroy()
        {
            CleanupConfigEditor();
            _creatorTab?.SavePreferences();
            _exceptionViewerTab?.Disable();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        #endregion

        #region Tab 切换

        private void SwitchTab(int index)
        {
            if (_activeTabIndex == index) return;

            // 取消旧 Tab 选中态
            if (_activeTabIndex >= 0 && _activeTabIndex < _tabToggles.Count)
                _tabToggles[_activeTabIndex].value = false;

            _activeTabIndex = index;

            // 激活新 Tab 选中态
            if (_activeTabIndex >= 0 && _activeTabIndex < _tabToggles.Count)
                _tabToggles[_activeTabIndex].SetValueWithoutNotify(true);

            // 切换内容
            _contentContainer.Clear();

            switch (_activeTabIndex)
            {
                case TAB_CONFIG_EDITOR:
                    _contentContainer.Add(_configEditorRoot);
                    _countLabel.style.display = DisplayStyle.Flex;
                    _countLabel.text = $"共 {_configs.Count} 个配置表";
                    break;

                case TAB_CONFIG_CREATOR:
                    _contentContainer.Add(_configCreatorRoot);
                    _countLabel.style.display = DisplayStyle.None;
                    break;

                case TAB_AUDIO_DEBUGGER:
                    _contentContainer.Add(_audioDebuggerRoot);
                    _countLabel.style.display = DisplayStyle.None;
                    break;

                case TAB_EXCEPTION_VIEWER:
                    _contentContainer.Add(_exceptionViewerRoot);
                    _countLabel.style.display = DisplayStyle.None;
                    _exceptionViewerTab?.Enable();
                    break;
            }
        }

        #endregion

        #region 运行时生命周期

        private void Update()
        {
            // 仅当音频调试 Tab 处于激活状态时才轮询
#if CFRAMEWORK_AUDIO
            if (_activeTabIndex == TAB_AUDIO_DEBUGGER && _audioDebuggerTab != null)
            {
                _audioDebuggerTab.Update();
            }
#endif
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 异常查看 Tab 需要感知 Play Mode 变化
            if (_exceptionViewerTab != null)
            {
                _exceptionViewerTab.OnPlayModeStateChanged(state);
            }

#if CFRAMEWORK_AUDIO
            // 音频调试 Tab 在进入 Play Mode 时需要激活，退出时清理缓存
            if (_audioDebuggerTab != null)
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                    _audioDebuggerTab.Update();
                else if (state == PlayModeStateChange.ExitingPlayMode)
                    _audioDebuggerTab.OnExitingPlayMode();
            }
#endif
        }

        #endregion

        #region 样式加载

        private static void LoadStyleSheets(VisualElement root)
        {
            var dashboardSS = EditorStyleSheet.Find("CFrameworkDashboardWindow.uss");
            if (dashboardSS != null) root.styleSheets.Add(dashboardSS);

            var editorSS = EditorStyleSheet.Find("ConfigEditorWindow.uss");
            if (editorSS != null) root.styleSheets.Add(editorSS);

            var creatorSS = EditorStyleSheet.Find("ConfigCreatorWindow.uss");
            if (creatorSS != null) root.styleSheets.Add(creatorSS);

            var audioSS = EditorStyleSheet.Find("AudioDebuggerWindow.uss");
            if (audioSS != null) root.styleSheets.Add(audioSS);
        }

        #endregion

        // ============================================================
        //  配置管理 Tab
        // ============================================================

        #region 配置管理 - 数据结构

        [Serializable]
        public sealed class ConfigInfo
        {
            public string Name;
            public string Type;
            public int Count;
            public string Path;
            public ScriptableObject Asset;
            public Type ConfigType;

            public override string ToString() => Name;
        }

        #endregion

        #region 配置管理 - 内容构建

        private VisualElement _configEditorRoot;

        private void InitConfigEditorContent()
        {
            _configEditorRoot = new VisualElement();
            _configEditorRoot.style.flexGrow = 1;

            // 工具栏
            var toolbar = new Toolbar();
            toolbar.AddToClassList("main-toolbar");

            var newConfigBtn = new Button(() =>
            {
                SwitchTab(TAB_CONFIG_CREATOR);
            })
            {
                text = "新建配置"
            };
            newConfigBtn.AddToClassList("toolbar-btn");
            toolbar.Add(newConfigBtn);

            var refreshBtn = new Button(RefreshConfigList) { text = "刷新" };
            refreshBtn.AddToClassList("toolbar-btn");
            toolbar.Add(refreshBtn);

            toolbar.Add(CreateFlexibleSpace());

            var countInToolbar = new Label("共 0 个配置表");
            countInToolbar.AddToClassList("count-label");
            toolbar.Add(countInToolbar);
            _editorTabCountLabel = countInToolbar;

            _configEditorRoot.Add(toolbar);

            // 分屏视图
            var splitView = new TwoPaneSplitView(0, 260, TwoPaneSplitViewOrientation.Horizontal);
            splitView.AddToClassList("split-view");
            _configEditorRoot.Add(splitView);

            // 左侧面板
            splitView.Add(CreateConfigListPanel());

            // 右侧面板
            splitView.Add(CreateConfigDetailPanel());

            // 延迟加载数据
            EditorApplication.delayCall += RefreshConfigList;
        }

        private Label _editorTabCountLabel;

        private VisualElement CreateConfigListPanel()
        {
            var leftPane = new VisualElement();
            leftPane.AddToClassList("left-pane");

            // 搜索栏
            _searchField = new ToolbarSearchField { name = "config-search" };
            _searchField.RegisterValueChangedCallback(evt => ApplySearchFilter(evt.newValue));
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
                    if (obj is int idx) SelectConfig(idx);
                    break;
                }
            };

            leftPane.Add(_configListView);
            return leftPane;
        }

        private VisualElement CreateConfigDetailPanel()
        {
            var rightPane = new VisualElement();
            rightPane.AddToClassList("right-pane");

            _detailTitleLabel = new Label("") { style = { display = DisplayStyle.None } };
            _detailTitleLabel.AddToClassList("detail-title");
            rightPane.Add(_detailTitleLabel);

            var pathRow = new VisualElement();
            pathRow.AddToClassList("path-row");
            var pathPrefix = new Label("路径:");
            pathPrefix.AddToClassList("path-prefix");
            pathRow.Add(pathPrefix);
            _pathLabel = new Label("");
            _pathLabel.AddToClassList("path-value");
            pathRow.Add(_pathLabel);
            pathRow.style.display = DisplayStyle.None;
            _pathRow = pathRow;
            rightPane.Add(pathRow);

            _divider = new VisualElement();
            _divider.AddToClassList("detail-divider");
            _divider.style.display = DisplayStyle.None;
            rightPane.Add(_divider);

            _rightScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            _rightScrollView.AddToClassList("content-scroll");
            rightPane.Add(_rightScrollView);

            return rightPane;
        }

        private VisualElement _pathRow;

        #endregion

        #region 配置管理 - 操作逻辑

        public void RefreshConfigList()
        {
            _configs.Clear();
            _selectedIndex = -1;
            _selectedConfig = null;
            CleanupConfigEditor();

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

            var countText = $"共 {_configs.Count} 个配置表";
            _editorTabCountLabel.text = countText;
            _countLabel.text = countText;

            _configListView.itemsSource = _configs;
            _configListView.RefreshItems();

            ShowEmptyState();
            Debug.Log($"[ConfigEditor] 找到 {_configs.Count} 个配置表");
        }

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

        private void SelectConfig(int index)
        {
            if (_selectedIndex == index && _selectedConfig != null) return;

            _selectedIndex = index;
            CleanupConfigEditor();

            if (index < 0 || index >= _configs.Count) return;

            var config = _configs[index];
            _selectedConfig = config.Asset as ConfigTableAsset;

            if (_selectedConfig == null) return;

            _serializedConfig = new SerializedObject(_selectedConfig);
            _configEditor = UnityEditor.Editor.CreateEditor(_selectedConfig);

            var configName = _selectedConfig.GetType().Name;
            _detailTitleLabel.text = _selectedIndex >= 0 && _selectedIndex < _configs.Count
                ? $"配置详情 - {configName} ({_configs[_selectedIndex].Count} 条记录)"
                : $"配置详情 - {configName}";
            _detailTitleLabel.style.display = DisplayStyle.Flex;

            _pathLabel.text = config.Path;
            _pathRow.style.display = !string.IsNullOrEmpty(config.Path)
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

        private void ShowConfigDetail()
        {
            _rightScrollView.Clear();

            if (_serializedConfig != null)
            {
                var inspector = new InspectorElement(_serializedConfig);
                _rightScrollView.Add(inspector);
            }
            else
            {
                _rightScrollView.Add(new Label("无法加载编辑器"));
            }
        }

        private void CleanupConfigEditor()
        {
            if (_configEditor != null)
            {
                DestroyImmediate(_configEditor);
                _configEditor = null;
            }

            _serializedConfig?.Dispose();
            _serializedConfig = null;
        }

        private static VisualElement CreateFlexibleSpace()
        {
            var space = new VisualElement();
            space.style.flexGrow = 1;
            return space;
        }

        #endregion

        #region 配置管理 - 列表项元素

        private class ConfigListItemElement : VisualElement
        {
            private readonly Label _nameLabel;
            private readonly Label _typeLabel;
            private readonly Label _countItemLabel;
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
                _countItemLabel = new Label();
                _countItemLabel.AddToClassList("config-count");
                metaRow.Add(_countItemLabel);
                Add(metaRow);

                RegisterCallback<ClickEvent>(_ => _onSelected?.Invoke(_boundInfo));
            }

            public void SetData(ConfigInfo info, bool isSelected, Action<ConfigInfo> onSelected)
            {
                _boundInfo = info;
                _onSelected = onSelected;

                _nameLabel.text = info.Name;
                _typeLabel.text = info.Type;
                _countItemLabel.text = $"{info.Count} 条";

                EnableInClassList("selected", isSelected);
            }
        }

        #endregion

        // ============================================================
        //  配置创建 Tab
        // ============================================================

        #region 配置创建 - 内容构建

        private VisualElement _configCreatorRoot;

        private void InitConfigCreatorContent()
        {
            _creatorTab = new DashboardConfigCreatorTab();
            _creatorTab.LoadPreferences();
            _configCreatorRoot = _creatorTab.CreateContent();
        }

        #endregion

        // ============================================================
        //  音频调试 Tab
        // ============================================================

        #region 音频调试 - 内容构建

        private void InitAudioDebuggerContent()
        {
#if CFRAMEWORK_AUDIO
            _audioDebuggerTab = new DashboardAudioDebuggerTab();
            _audioDebuggerRoot = _audioDebuggerTab.CreateContent();
#else
            // 未启用 CFRAMEWORK_AUDIO 时显示提示
            _audioDebuggerRoot = CreateDisabledTabContent(
                "音频调试",
                "需要启用 CFRAMEWORK_AUDIO 脚本定义符号才能使用此功能。\n\n请在 Project Settings > Player > Scripting Define Symbols 中添加 CFRAMEWORK_AUDIO。"
            );
#endif
        }

        #endregion

        // ============================================================
        //  异常查看 Tab
        // ============================================================

        #region 异常查看 - 内容构建

        private void InitExceptionViewerContent()
        {
            _exceptionViewerTab = new DashboardExceptionViewerTab();
            _exceptionViewerRoot = _exceptionViewerTab.CreateContent();
        }

        #endregion

        // ============================================================
        //  通用工具
        // ============================================================

        #region 通用工具

        /// <summary>
        ///     创建功能不可用时的提示内容
        /// </summary>
        private static VisualElement CreateDisabledTabContent(string title, string message)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;

            var box = new VisualElement();
            box.style.maxWidth = 400;
            box.style.paddingTop = 20;
            box.style.paddingBottom = 20;
            box.style.paddingLeft = 20;
            box.style.paddingRight = 20;

            var titleLabel = new Label($"{title} 不可用");
            titleLabel.style.fontSize = 14;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(titleLabel);

            var messageLabel = new Label(message);
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(messageLabel);

            container.Add(box);
            return container;
        }

        #endregion
    }
}
