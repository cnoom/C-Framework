#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Windows.Config
{
    /// <summary>
    ///     配置管理编辑器窗口
    /// </summary>
    public sealed class ConfigEditorWindow : OdinEditorWindow
    {
        #region 菜单项

                /// <summary>
        ///     打开窗口（由 Dashboard 调用）
        /// </summary>
        public static void OpenWindow()
        {
            var window = GetWindow<ConfigEditorWindow>("配置管理");
            window.position = new Rect(100, 100, 1200, 700);
            window.Show();
        }

        #endregion

        #region 数据结构

        /// <summary>
        ///     配置信息
        /// </summary>
        [Serializable]
        public sealed class ConfigInfo
        {
            [DisplayAsString] [LabelText("名称")] public string Name;

            [DisplayAsString] [LabelText("类型")] public string Type;

            [DisplayAsString] [LabelText("数量")] public int Count;

            [HideInInspector] public string Path;

            [HideInInspector] public ScriptableObject Asset;

            public Type ConfigType;

            public override string ToString()
            {
                return Name;
            }
        }

        #endregion

        #region 字段

        [HorizontalGroup("Main", 0.25f)]
        [BoxGroup("Main/左侧", ShowLabel = false)]
        [ListDrawerSettings(
            ShowPaging = true,
            NumberOfItemsPerPage = 20,
            IsReadOnly = true,
            OnTitleBarGUI = nameof(DrawRefreshButton),
            ShowIndexLabels = false,
            DraggableItems = false
        )]
        [LabelText("配置表列表")]
        [OnValueChanged(nameof(OnSelectedConfigChanged))]
        [PropertyOrder(-100)]
        public List<ConfigInfo> existingConfigs = new();

        private ConfigInfo _selectedConfigInfo;
        private PropertyTree _configTree;

        [HorizontalGroup("Main", 0.75f)]
        [BoxGroup("Main/右侧")]
        [LabelText("$CurrentConfigTitle")]
        [ShowInInspector]
        [PropertyOrder(100)]
        [ShowIf(nameof(HasSelectedConfig))]
        private ConfigTableAsset _selectedConfig;

        [HorizontalGroup("Main", 0.75f)]
        [BoxGroup("Main/右侧")]
        [PropertyOrder(99)]
        [ShowIf(nameof(ShowEmptyState))]
        [HideLabel]
        [DisplayAsString(UnityEngine.TextAlignment.Center)]
        private string EmptyStateMessage => existingConfigs.Count == 0
            ? "暂无配置表\n\n请点击下方「新建配置」按钮创建配置表"
            : "请在左侧选择一个配置表";

        private bool ShowEmptyState => !HasSelectedConfig;

        private bool HasSelectedConfig => _selectedConfig != null;

        private string CurrentConfigTitle => _selectedConfigInfo != null
            ? $"配置详情 - {_selectedConfigInfo.Name} ({_selectedConfigInfo.Count} 条记录)"
            : "配置详情";

        #endregion

        #region 初始化

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshConfigList();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _configTree?.Dispose();
        }

        #endregion

        #region 操作按钮

        private void DrawRefreshButton()
        {
            if (GUILayout.Button("刷新", GUILayout.Width(60))) RefreshConfigList();
        }

        [Button("新建配置", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void CreateNewConfig()
        {
            ConfigCreatorWindow.OpenWindow();
        }

        #endregion

        #region 刷新逻辑

        public void RefreshConfigList()
        {
            existingConfigs.Clear();
            _selectedConfig = null;
            _selectedConfigInfo = null;

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
                        existingConfigs.Add(new ConfigInfo
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

            Debug.Log($"[ConfigEditor] 找到 {existingConfigs.Count} 个配置表");
        }

        private void OnSelectedConfigChanged()
        {
            // 清除之前的选择
            _selectedConfig = null;
            _selectedConfigInfo = null;
            _configTree?.Dispose();
            _configTree = null;

            if (existingConfigs == null || existingConfigs.Count == 0) return;

            // 查找列表中被选中的配置（通过检查 Odin 列表的内部选择状态）
            // 由于 Odin 的 ListDrawerSettings 不直接提供选中索引，我们需要遍历检查
            // 使用 Selection 作为辅助判断
            var selectedAsset = Selection.activeObject as ScriptableObject;

            for (var i = 0; i < existingConfigs.Count; i++)
            {
                var configInfo = existingConfigs[i];
                if (configInfo.Asset == selectedAsset)
                {
                    _selectedConfigInfo = configInfo;
                    _selectedConfig = configInfo.Asset as ConfigTableAsset;

                    // 创建属性树用于编辑
                    _configTree?.Dispose();
                    _configTree = PropertyTree.Create(_selectedConfig);
                    return;
                }
            }
        }

        protected override void DrawEditors()
        {
            // 绘制顶部工具栏
            SirenixEditorGUI.BeginHorizontalToolbar();
            {
                if (SirenixEditorGUI.ToolbarButton(new GUIContent("新建配置", "创建新的配置表")))
                    CreateNewConfig();

                GUILayout.Space(10);

                if (SirenixEditorGUI.ToolbarButton(new GUIContent("刷新", "刷新配置表列表")))
                    RefreshConfigList();

                GUILayout.FlexibleSpace();

                // 显示配置表数量
                GUILayout.Label($"共 {existingConfigs.Count} 个配置表", EditorStyles.miniLabel);
            }
            SirenixEditorGUI.EndHorizontalToolbar();

            // 如果没有配置表，显示提示
            if (existingConfigs.Count == 0)
            {
                DrawEmptyState();
                return;
            }

            // 调用基类绘制 Odin 管理的属性
            base.DrawEditors();
        }

        private void DrawEmptyState()
        {
            var rect = GUILayoutUtility.GetRect(0, 200);
            rect = EditorGUI.IndentedRect(rect);

            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));

            // 绘制提示文字
            var contentRect = rect;
            contentRect.height = 40;
            contentRect.y = rect.center.y - 20;

            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                wordWrap = true
            };

            GUI.Label(contentRect, "暂无配置表\n请点击「新建配置」按钮创建", style);
        }

        #endregion
    }
}
#endif