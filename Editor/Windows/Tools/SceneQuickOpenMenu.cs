using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CFramework.Editor.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows.Tools
{
    /// <summary>
    ///     场景快捷跳转菜单（UIToolkit 实现）
    /// </summary>
    public static class SceneQuickOpenMenu
    {
        private const string MenuRoot = "Scene/";

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
            if (obj != null) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); }
        }

        [MenuItem(MenuRoot + "Switch to Scene...")]
        public static void ShowSceneSwitchWindow() => SceneQuickOpenWindow.Open();

        internal static void OpenScene(string scenePath)
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                var result = EditorUtility.DisplayDialogComplex("场景未保存",
                    $"当前场景 \"{activeScene.name}\" 有未保存的修改。\n是否保存后再打开新场景？",
                    "保存并打开", "不保存直接打开", "取消");
                switch (result)
                { case 0: EditorSceneManager.SaveScene(activeScene); break; case 1: break; case 2: return; }
            }
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        internal static List<string> GetAllScenePaths() =>
            AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(path => path.EndsWith(".unity")).OrderBy(path => path).ToList();
    }

    /// <summary>
    ///     场景快捷选择窗口（UIToolkit 实现）
    /// </summary>
    public sealed class SceneQuickOpenWindow : EditorWindow
    {
        private const string USS_FILE_NAME = "SceneQuickOpenMenu.uss";

        private List<string> _scenePaths;
        private List<string> _filteredScenes;

        private TextField _searchField;
        private Button _refreshButton;
        private Label _currentSceneLabel;
        private ListView _sceneListView;
        private Label _footerLabel;

        internal static void Open()
        {
            var window = GetWindow<SceneQuickOpenWindow>("Scene Quick Open");
            window.minSize = new Vector2(320, 400);
            window.Show();
            window.Focus();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 加载 USS 样式表
            var styleSheet = EditorStyleSheet.Find(USS_FILE_NAME);
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            // 搜索栏容器
            var searchRow = new VisualElement();
            searchRow.AddToClassList("search-row");

            _searchField = new TextField();
            _searchField.AddToClassList("search-field");
            _searchField.RegisterValueChangedCallback(evt => FilterScenes(evt.newValue));
            searchRow.Add(_searchField);

            _refreshButton = new Button(() =>
            {
                _scenePaths = SceneQuickOpenMenu.GetAllScenePaths();
                FilterScenes(_searchField.value);
            })
            { text = "\u21bb" };
            _refreshButton.AddToClassList("refresh-button");
            searchRow.Add(_refreshButton);

            root.Add(searchRow);

            // 当前场景信息
            var activeScene = SceneManager.GetActiveScene();
            _currentSceneLabel = new Label($"当前场景: {activeScene.name}");
            _currentSceneLabel.AddToClassList("current-scene-label");
            root.Add(_currentSceneLabel);

            // 场景列表
            _sceneListView = new ListView
            {
                makeItem = () => new SceneItemElement(),
                bindItem = (element, index) =>
                {
                    if (element is SceneItemElement itemElement)
                    {
                        var scenePath = _filteredScenes[index];
                        var isActive = SceneManager.GetActiveScene().path == scenePath;
                        itemElement.SetData(scenePath, isActive,
                            s => { Close(); SceneQuickOpenMenu.OpenScene(s); });
                    }
                },
                selectionType = SelectionType.None,
                showBorder = false,
                fixedItemHeight = 28
            };
            _sceneListView.AddToClassList("scene-list");
            root.Add(_sceneListView);

            // 底部提示
            _footerLabel = new Label("点击切换 | ESC 关闭");
            _footerLabel.AddToClassList("footer-label");
            root.Add(_footerLabel);

            // 注册键盘事件
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnEnable()
        {
            _scenePaths = SceneQuickOpenMenu.GetAllScenePaths();
            _filteredScenes = new List<string>(_scenePaths);

            EditorApplication.delayCall += () =>
            {
                if (_sceneListView != null)
                {
                    _sceneListView.itemsSource = _filteredScenes;
                    _sceneListView.RefreshItems();
                    UpdateFooterCount();
                }
            };
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape) { Close(); evt.StopPropagation(); }
            else if (evt.keyCode == KeyCode.Return && _filteredScenes.Count > 0)
            { Close(); SceneQuickOpenMenu.OpenScene(_filteredScenes[0]); evt.StopPropagation(); }
        }

        private void FilterScenes(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) _filteredScenes = new List<string>(_scenePaths);
            else
            {
                var keyword = searchText.ToLowerInvariant();
                _filteredScenes = _scenePaths.Where(p =>
                {
                    var name = Path.GetFileNameWithoutExtension(p);
                    return name.ToLowerInvariant().Contains(keyword) || p.ToLowerInvariant().Contains(keyword);
                }).ToList();
            }
            _sceneListView.itemsSource = _filteredScenes;
            _sceneListView.RefreshItems();
            UpdateFooterCount();
        }

        private void UpdateFooterCount() => _footerLabel.text = $"共 {_filteredScenes.Count} 个场景  |  点击切换 | ESC 关闭";

        #region 场景列表项元素

        private class SceneItemElement : VisualElement
        {
            private readonly VisualElement _iconContainer;
            private readonly Label _nameLabel;
            private readonly Label _pathLabel;
            private string _boundScenePath;
            private Action<string> _onSelected;

            public SceneItemElement()
            {
                AddToClassList("scene-item");

                _iconContainer = new VisualElement();
                _iconContainer.AddToClassList("scene-item-icon");
                Add(_iconContainer);

                _nameLabel = new Label();
                _nameLabel.AddToClassList("scene-item-name");
                Add(_nameLabel);

                _pathLabel = new Label();
                _pathLabel.AddToClassList("scene-item-path");
                Add(_pathLabel);

                RegisterCallback<ClickEvent>(_ => _onSelected?.Invoke(_boundScenePath));
            }

            public void SetData(string scenePath, bool isActive, Action<string> onSelected)
            {
                _boundScenePath = scenePath;
                _onSelected = onSelected;

                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                _nameLabel.text = isActive ? $"\u25CF {sceneName}" : sceneName;

                if (isActive)
                {
                    _nameLabel.style.color = new Color(0.31f, 0.85f, 0.31f);
                    style.backgroundColor = new Color(0.1f, 0.25f, 0.1f, 0.15f);
                }
                else
                {
                    _nameLabel.style.color = new Color(0.86f, 0.86f, 0.86f);
                    style.backgroundColor = new StyleColor(Color.clear);
                }

                var dirName = Path.GetDirectoryName(scenePath)?.Replace("Assets/", "");
                _pathLabel.text = dirName ?? "";
                _pathLabel.style.display = !string.IsNullOrEmpty(dirName) ? DisplayStyle.Flex : DisplayStyle.None;

                _iconContainer.style.backgroundColor = isActive
                    ? new StyleColor(new Color(0.25f, 0.55f, 0.25f))
                    : new StyleColor(new Color(0.35f, 0.35f, 0.4f));
            }
        }

        #endregion
    }
}
