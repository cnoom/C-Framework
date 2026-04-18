using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private List<string> _scenePaths;
        private List<string> _filteredScenes;

        // UIToolkit 控件
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

            // 搜索栏容器
            var searchRow = new VisualElement();
            searchRow.style.flexDirection = FlexDirection.Row;
            searchRow.style.alignItems = Align.Center;
            searchRow.style.paddingTop = 6;
            searchRow.style.paddingBottom = 6;
            searchRow.style.paddingLeft = 8;
            searchRow.style.paddingRight = 8;
            searchRow.style.marginBottom = 6;
            searchRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.22f, 0.9f);

            _searchField = new TextField();
            _searchField.style.flexGrow = 1;
            _searchField.style.marginRight = 4;
            _searchField.RegisterValueChangedCallback(evt => FilterScenes(evt.newValue));
            searchRow.Add(_searchField);

            _refreshButton = new Button(() =>
            {
                _scenePaths = SceneQuickOpenMenu.GetAllScenePaths();
                FilterScenes(_searchField.value);
            })
            { text = "\u21bb" };
            _refreshButton.style.width = 24;
            _refreshButton.style.height = 24;
            _refreshButton.style.fontSize = 14;
            _refreshButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            searchRow.Add(_refreshButton);

            root.Add(searchRow);

            // 当前场景信息
            var activeScene = SceneManager.GetActiveScene();
            _currentSceneLabel = new Label($"当前场景: {activeScene.name}");
            _currentSceneLabel.style.fontSize = 11;
            _currentSceneLabel.style.color = new Color(0.63f, 0.71f, 0.63f);
            _currentSceneLabel.style.paddingTop = 4;
            _currentSceneLabel.style.paddingBottom = 4;
            _currentSceneLabel.style.paddingLeft = 10;
            _currentSceneLabel.style.marginBottom = 4;
            _currentSceneLabel.style.backgroundColor = new Color(0.18f, 0.22f, 0.18f, 0.5f);
            _currentSceneLabel.style.unityTextAlign = TextAnchor.UpperLeft;
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
            _sceneListView.style.flexGrow = 1;
            _sceneListView.style.paddingLeft = 4;
            _sceneListView.style.paddingRight = 4;
            root.Add(_sceneListView);

            // 底部提示
            _footerLabel = new Label("点击切换 | ESC 关闭");
            _footerLabel.style.fontSize = 10;
            _footerLabel.style.color = new Color(0.47f, 0.47f, 0.47f);
            _footerLabel.style.paddingTop = 4;
            _footerLabel.style.paddingBottom = 4;
            _footerLabel.style.paddingLeft = 10;
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
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;
                style.paddingLeft = 8;
                style.paddingRight = 8;

                _iconContainer = new VisualElement();
                _iconContainer.style.width = 16;
                _iconContainer.style.height = 16;
                _iconContainer.style.marginRight = 6;
                Add(_iconContainer);

                _nameLabel = new Label();
                _nameLabel.style.fontSize = 12;
                _nameLabel.style.flexGrow = 1;
                _nameLabel.style.unityTextAlign = TextAnchor.UpperLeft;
                Add(_nameLabel);

                _pathLabel = new Label();
                _pathLabel.style.fontSize = 10;
                _pathLabel.style.color = new Color(0.43f, 0.43f, 0.43f);
                _pathLabel.style.marginLeft = 8;
                Add(_pathLabel);

                RegisterCallback<ClickEvent>(_ => _onSelected?.Invoke(_boundScenePath));
            }

            public void SetData(string scenePath, bool isActive, Action<string> onSelected)
            {
                _boundScenePath = scenePath;
                _onSelected = onSelected;

                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                _nameLabel.text = isActive ? $"\u25CF {sceneName}" : sceneName;  // ●

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
