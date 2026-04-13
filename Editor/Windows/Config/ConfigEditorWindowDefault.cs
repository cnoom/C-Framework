#if !ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CFramework.Editor.Windows.Config
{
    /// <summary>
    ///     配置管理编辑器窗口（默认实现，不依赖 Odin）
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

        #region 字段

        private List<ConfigInfo> _configs = new();
        private int _selectedIndex = -1;
        private Vector2 _leftScrollPos;
        private Vector2 _rightScrollPos;
        private string _searchFilter = "";

        // 选中配置的编辑器
        private Editor _configEditor;
        private SerializedObject _serializedConfig;
        private ReorderableList _reorderableList;
        private ConfigTableBase _selectedConfig;

        // 样式
        private GUIStyle _selectedItemStyle;
        private GUIStyle _normalItemStyle;
        private GUIStyle _headerStyle;
        private bool _stylesInitialized;

        #endregion

        #region 生命周期

        private void OnEnable()
        {
            RefreshConfigList();
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

        #region GUI

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLeftPanel();
                DrawRightPanel();
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _selectedItemStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 6, 6)
            };

            _normalItemStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 6, 6)
            };

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("新建配置", EditorStyles.toolbarButton))
                {
                    ConfigCreatorWindow.OpenWindow();
                }

                if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
                {
                    RefreshConfigList();
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label($"共 {_configs.Count} 个配置表", EditorStyles.miniLabel);
            }
        }

        private void DrawLeftPanel()
        {
            using (new EditorGUILayout.VerticalScope("OL Box", GUILayout.Width(260)))
            {
                // 搜索框
                _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
                EditorGUILayout.Space(2);

                // 配置列表
                _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos);

                for (var i = 0; i < _configs.Count; i++)
                {
                    var config = _configs[i];

                    // 搜索过滤
                    if (!string.IsNullOrEmpty(_searchFilter) &&
                        !config.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var isSelected = i == _selectedIndex;
                    var style = isSelected ? _selectedItemStyle : _normalItemStyle;

                    var rect = EditorGUILayout.BeginVertical(style);

                    // 选中高亮
                    if (isSelected)
                    {
                        EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), new Color(0.24f, 0.49f, 0.75f, 0.3f));
                    }

                    EditorGUILayout.LabelField(config.Name, EditorStyles.boldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(config.Type, EditorStyles.miniLabel);
                        GUILayout.Label($"{config.Count} 条", EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.EndVertical();

                    // 点击选择
                    var currentEvent = Event.current;
                    if (currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition))
                    {
                        SelectConfig(i);
                        currentEvent.Use();
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawRightPanel()
        {
            using (new EditorGUILayout.VerticalScope("OL Box"))
            {
                if (_selectedConfig != null)
                {
                    DrawConfigDetail();
                }
                else
                {
                    DrawEmptyState();
                }
            }
        }

        private void DrawConfigDetail()
        {
            // 配置标题
            var configName = _selectedConfig.GetType().Name;
            var configInfo = _selectedIndex >= 0 && _selectedIndex < _configs.Count ? _configs[_selectedIndex] : null;
            var title = configInfo != null
                ? $"配置详情 - {configName} ({configInfo.Count} 条记录)"
                : $"配置详情 - {configName}";

            EditorGUILayout.LabelField(title, _headerStyle);
            EditorGUILayout.Space(4);

            // 资产路径
            if (configInfo != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("路径:", EditorStyles.miniLabel, GUILayout.Width(35));
                    GUILayout.Label(configInfo.Path, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(2);

            // 分割线
            var lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 1f));
            EditorGUILayout.Space(4);

            // 使用 ReorderableList 编辑数据
            if (_reorderableList != null)
            {
                _serializedConfig?.Update();
                _reorderableList.DoLayoutList();
                _serializedConfig?.ApplyModifiedProperties();
            }
            else
            {
                // 备用：使用内置 Inspector
                _rightScrollPos = EditorGUILayout.BeginScrollView(_rightScrollPos);
                if (_configEditor != null)
                {
                    _configEditor.serializedObject.Update();
                    _configEditor.OnInspectorGUI();
                    _configEditor.serializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var message = _configs.Count == 0
                    ? "暂无配置表\n请点击「新建配置」按钮创建"
                    : "请在左侧选择一个配置表";

                var style = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    wordWrap = true
                };

                EditorGUILayout.LabelField(message, style, GUILayout.Width(300), GUILayout.Height(60));
                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        #endregion

        #region 操作

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
            var dataListProp = _serializedConfig.FindProperty("dataList");

            if (dataListProp != null)
            {
                _reorderableList = new ReorderableList(_serializedConfig, dataListProp, true, true, true, true);

                _reorderableList.drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect,
                        $"数据列表 ({dataListProp.arraySize} 条)",
                        EditorStyles.boldLabel);
                };

                _reorderableList.drawElementCallback = (rect, idx, isActive, isFocused) =>
                {
                    var element = dataListProp.GetArrayElementAtIndex(idx);
                    rect.y += 2;
                    rect.height -= 4;

                    // 绘制元素的所有子属性
                    DrawElementFields(rect, element, idx);
                };

                _reorderableList.elementHeightCallback = idx =>
                {
                    var element = dataListProp.GetArrayElementAtIndex(idx);
                    return GetElementHeight(element) + 8;
                };

                _reorderableList.onAddCallback = list =>
                {
                    var index1 = list.serializedProperty.arraySize;
                    list.serializedProperty.arraySize++;
                    list.index = index1;
                    _serializedConfig?.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_selectedConfig);
                };

                _reorderableList.onRemoveCallback = list =>
                {
                    list.serializedProperty.DeleteArrayElementAtIndex(list.index);
                    _serializedConfig?.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_selectedConfig);
                };

                _reorderableList.onChangedCallback = list =>
                {
                    EditorUtility.SetDirty(_selectedConfig);
                };
            }
            else
            {
                // 备用方案：使用内置 Inspector
                _configEditor = Editor.CreateEditor(_selectedConfig);
            }

            Repaint();
        }

        /// <summary>
        ///     绘制元素的所有可见子属性
        /// </summary>
        private void DrawElementFields(Rect rect, SerializedProperty element, int index)
        {
            var child = element.Copy();
            var endProp = element.GetEndProperty();
            var y = rect.y;
            var x = rect.x;

            // 尝试水平排列前几个简单属性
            var fields = new List<SerializedProperty>();
            if (child.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(child, endProp))
                {
                    fields.Add(child.Copy());
                    if (!child.NextVisible(false)) break;
                }
            }

            if (fields.Count <= 4)
            {
                // 字段较少时，水平排列
                var widthPerField = rect.width / fields.Count;
                for (var i = 0; i < fields.Count; i++)
                {
                    var fieldRect = new Rect(x, y, widthPerField - 4, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(fieldRect, fields[i], GUIContent.none);
                    x += widthPerField;
                }
            }
            else
            {
                // 字段较多时，垂直排列
                foreach (var field in fields)
                {
                    var height = EditorGUI.GetPropertyHeight(field);
                    var fieldRect = new Rect(rect.x, y, rect.width, height);
                    EditorGUI.PropertyField(fieldRect, field, true);
                    y += height + 2;
                }
            }
        }

        /// <summary>
        ///     计算元素高度
        /// </summary>
        private float GetElementHeight(SerializedProperty element)
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

            Debug.Log($"[ConfigEditor] 找到 {_configs.Count} 个配置表");
            Repaint();
        }

        private void CleanupEditor()
        {
            _reorderableList = null;

            if (_configEditor != null)
            {
                DestroyImmediate(_configEditor);
                _configEditor = null;
            }

            _serializedConfig?.Dispose();
            _serializedConfig = null;
        }

        #endregion
    }
}
#endif
