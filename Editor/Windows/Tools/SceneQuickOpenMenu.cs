using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CFramework.Editor.Windows.Tools
{
    /// <summary>
    ///     场景快捷跳转菜单
    ///     在菜单栏 Scene/ 下提供场景快速切换和定位当前场景文件夹功能
    /// </summary>
    public static class SceneQuickOpenMenu
    {
        private const string MenuRoot = "Scene/";

        /// <summary>
        ///     定位当前场景所在文件夹（菜单栏顶级按钮）
        /// </summary>
        [MenuItem(MenuRoot + "Locate Current Scene Folder")]
        public static void LocateCurrentSceneFolder()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                Debug.LogWarning("[SceneQuickOpen] 当前场景未保存，无法定位文件夹");
                return;
            }

            var folder = Path.GetDirectoryName(activeScene.path);
            if (string.IsNullOrEmpty(folder)) return;

            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }

        /// <summary>
        ///     打开场景选择窗口
        /// </summary>
        [MenuItem(MenuRoot + "Switch to Scene...")]
        public static void ShowSceneSwitchWindow()
        {
            SceneQuickOpenWindow.Open();
        }

        /// <summary>
        ///     打开指定场景，若当前场景未保存则询问
        /// </summary>
        internal static void OpenScene(string scenePath)
        {
            var activeScene = SceneManager.GetActiveScene();

            if (activeScene.isDirty)
            {
                var result = EditorUtility.DisplayDialogComplex(
                    "场景未保存",
                    $"当前场景 \"{activeScene.name}\" 有未保存的修改。\n是否保存后再打开新场景？",
                    "保存并打开",
                    "不保存直接打开",
                    "取消");

                switch (result)
                {
                    case 0:
                        EditorSceneManager.SaveScene(activeScene);
                        break;
                    case 1:
                        break;
                    case 2:
                        return;
                }
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        /// <summary>
        ///     获取 Assets 下所有场景路径
        /// </summary>
        internal static List<string> GetAllScenePaths()
        {
            return AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".unity"))
                .OrderBy(path => path)
                .ToList();
        }
    }

    /// <summary>
    ///     场景快捷选择窗口
    ///     带搜索功能的场景列表，点击即可切换场景
    /// </summary>
    public sealed class SceneQuickOpenWindow : EditorWindow
    {
        private const float ItemHeight = 26f;

        private List<string> _scenePaths;
        private List<string> _filteredScenes;
        private string _searchText;
        private Vector2 _scrollPosition;
        private int _hoveredIndex = -1;
        private GUIStyle _itemStyle;
        private GUIStyle _hoverStyle;
        private GUIStyle _activeStyle;
        private GUIStyle _pathStyle;
        private bool _stylesInitialized;

        internal static void Open()
        {
            var window = GetWindow<SceneQuickOpenWindow>("Scene Quick Open");
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            _searchText = string.Empty;
            _scenePaths = SceneQuickOpenMenu.GetAllScenePaths();
            _filteredScenes = new List<string>(_scenePaths);
            _hoveredIndex = -1;
        }

        private void OnGUI()
        {
            InitStyles();

            // 在搜索框获得焦点前拦截快捷键
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    Close();
                    e.Use();
                    return;
                }

                // Enter 打开第一个匹配的场景
                if (e.keyCode == KeyCode.Return && _filteredScenes.Count > 0)
                {
                    Close();
                    SceneQuickOpenMenu.OpenScene(_filteredScenes[0]);
                    e.Use();
                    return;
                }
            }

            // 顶部区域：搜索栏 + 刷新按钮
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope("HelpBox", GUILayout.Height(28)))
            {
                EditorGUILayout.Space(4);

                // 搜索图标
                GUILayout.Label(EditorGUIUtility.IconContent("d_Search Icon"), GUILayout.Width(20),
                    GUILayout.Height(20));

                GUI.SetNextControlName("SearchField");
                var newText = EditorGUILayout.TextField(_searchText, GUI.skin.textField);
                if (newText != _searchText)
                {
                    _searchText = newText;
                    FilterScenes();
                }

                if (GUILayout.Button("↻", GUI.skin.button, GUILayout.Width(24), GUILayout.Height(22)))
                {
                    _scenePaths = SceneQuickOpenMenu.GetAllScenePaths();
                    FilterScenes();
                }
            }

            EditorGUI.FocusTextInControl("SearchField");

            // 当前场景信息栏
            var activeScene = SceneManager.GetActiveScene();
            using (new EditorGUILayout.HorizontalScope("HelpBox", GUILayout.Height(22)))
            {
                EditorGUILayout.Space(4);
                GUILayout.Label($"当前场景: {activeScene.name}", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
            }

            // 分割线
            EditorGUILayout.Space(2);

            // 场景列表
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (var i = 0; i < _filteredScenes.Count; i++)
            {
                DrawSceneItem(_filteredScenes[i], i, activeScene);
            }

            if (_filteredScenes.Count == 0)
            {
                EditorGUILayout.Space(30);
                GUILayout.Label("未找到匹配的场景", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();

            // 底部提示栏
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"共 {_filteredScenes.Count} 个场景", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label("点击切换 | ESC 关闭", EditorStyles.miniLabel);
            }
        }

        private void DrawSceneItem(string scenePath, int index, Scene activeScene)
        {
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var isActive = activeScene.path == scenePath;
            var isHovered = _hoveredIndex == index;

            // 获取列表项区域，宽度撑满整行
            var itemRect = GUILayoutUtility.GetRect(
                new GUIContent(sceneName),
                EditorStyles.label,
                GUILayout.Height(ItemHeight),
                GUILayout.ExpandWidth(true));

            // 将宽度扩展到窗口可视区域宽度
            itemRect.width = position.width - 2f;

            var e = Event.current;

            // 鼠标悬停检测
            if (itemRect.Contains(e.mousePosition))
            {
                if (_hoveredIndex != index)
                {
                    _hoveredIndex = index;
                    Repaint();
                }
            }
            else if (_hoveredIndex == index)
            {
                _hoveredIndex = -1;
            }

            // 背景绘制
            if (isActive)
            {
                EditorGUI.DrawRect(itemRect, new Color(0.2f, 0.5f, 0.2f, 0.2f));
            }
            else if (isHovered)
            {
                EditorGUI.DrawRect(itemRect, new Color(0.3f, 0.6f, 1f, 0.15f));
            }

            // 左侧图标
            var iconRect = new Rect(itemRect.x + 6, itemRect.y + (itemRect.height - 16) * 0.5f, 16, 16);
            var icon = isActive
                ? EditorGUIUtility.IconContent("d_SceneAsset Icon").image
                : EditorGUIUtility.IconContent("SceneAsset Icon").image;
            GUI.DrawTexture(iconRect, icon);

            // 场景名称
            var nameRect = new Rect(iconRect.xMax + 6, itemRect.y, itemRect.width * 0.55f, itemRect.height);
            var label = isActive ? $"● {sceneName}" : sceneName;
            var style = isActive ? _activeStyle : isHovered ? _hoverStyle : _itemStyle;
            GUI.Label(nameRect, label, style);

            // 右侧路径
            var dirName = Path.GetDirectoryName(scenePath)?.Replace("Assets/", "");
            if (!string.IsNullOrEmpty(dirName))
            {
                var pathContent = new GUIContent(dirName);
                var pathSize = _pathStyle.CalcSize(pathContent);
                var pathRect = new Rect(itemRect.xMax - pathSize.x - 10,
                    itemRect.y + (itemRect.height - pathSize.y) * 0.5f,
                    pathSize.x, pathSize.y);
                GUI.Label(pathRect, dirName, _pathStyle);
            }

            // 整行点击事件（左键按下时判定）
            if (e.type == EventType.MouseDown
                && e.button == 0
                && itemRect.Contains(e.mousePosition))
            {
                Close();
                SceneQuickOpenMenu.OpenScene(scenePath);
                e.Use();
            }
        }

        private void FilterScenes()
        {
            _hoveredIndex = -1;

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                _filteredScenes = new List<string>(_scenePaths);
            }
            else
            {
                var keyword = _searchText.ToLowerInvariant();
                _filteredScenes = _scenePaths
                    .Where(path =>
                    {
                        var name = Path.GetFileNameWithoutExtension(path);
                        return name.ToLowerInvariant().Contains(keyword)
                               || path.ToLowerInvariant().Contains(keyword);
                    })
                    .ToList();
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _itemStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                richText = true
            };

            _hoverStyle = new GUIStyle(_itemStyle)
            {
                normal = { textColor = new Color(0.3f, 0.7f, 1f) }
            };

            _activeStyle = new GUIStyle(_itemStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.85f, 0.4f) }
            };

            _pathStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
        }
    }
}