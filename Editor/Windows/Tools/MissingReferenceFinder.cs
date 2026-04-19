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
    ///     丢失引用查找器（UIToolkit 实现）
    /// </summary>
    public class MissingReferenceFinder : EditorWindow
    {
        private const string USS_FILE_NAME = "MissingReferenceFinder.uss";

        private List<ResultItem> results = new List<ResultItem>();

        private Toggle _detectFieldToggle;
        private Button _scanButton;
        private Label _countLabel;
        private ScrollView _resultScroll;

        private bool scanInProgress = false;
        private bool detectFieldMissing = true;
        private Dictionary<MissingType, bool> foldoutStates = new Dictionary<MissingType, bool>
        {
            { MissingType.MissingScript, true },
            { MissingType.MissingFieldReference, true }
        };

        [MenuItem("CFramework/查找丢失引用物体")]
        public static void ShowWindow()
        {
            var window = GetWindow<MissingReferenceFinder>("丢失引用查找器");
            window.minSize = new Vector2(450, 400);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 加载 USS 样式表
            var styleSheet = EditorStyleSheet.Find(USS_FILE_NAME);
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            // 标题
            var titleLabel = new Label("扫描丢失引用");
            titleLabel.AddToClassList("title-label");
            root.Add(titleLabel);

            // 检测选项
            _detectFieldToggle = new Toggle("检测字段引用丢失 (可能误报)");
            _detectFieldToggle.value = detectFieldMissing;
            _detectFieldToggle.AddToClassList("detect-toggle");
            _detectFieldToggle.RegisterValueChangedCallback(evt =>
            {
                detectFieldMissing = evt.newValue;
            });
            root.Add(_detectFieldToggle);

            // 扫描按钮
            _scanButton = new Button(ScanAllAssets);
            _scanButton.text = "开始扫描";
            _scanButton.AddToClassList("scan-button");
            root.Add(_scanButton);

            // 结果计数
            _countLabel = new Label("共找到 0 个存在丢失引用的物体");
            _countLabel.AddToClassList("count-label");
            root.Add(_countLabel);

            // 结果滚动列表
            _resultScroll = new ScrollView();
            _resultScroll.AddToClassList("result-scroll");
            root.Add(_resultScroll);

            RefreshResults();
        }

        private void RefreshResults()
        {
            _countLabel.text = $"共找到 {results.Count} 个存在丢失引用的物体";
            _resultScroll.Clear();

            foreach (MissingType missingType in System.Enum.GetValues(typeof(MissingType)))
            {
                var group = results.Where(r => r.missingType == missingType).ToList();
                if (group.Count == 0) continue;

                if (!foldoutStates.ContainsKey(missingType))
                    foldoutStates[missingType] = true;

                var foldout = new Foldout();
                foldout.text = $"{GetMissingTypeName(missingType)} ({group.Count})";
                foldout.value = foldoutStates[missingType];
                foldout.AddToClassList("result-foldout");
                foldout.RegisterValueChangedCallback(evt =>
                {
                    foldoutStates[missingType] = evt.newValue;
                });

                foreach (var item in group)
                {
                    var itemRow = new VisualElement();
                    itemRow.AddToClassList("result-item-row");

                    var itemNameLabel = new Label(item.displayName);
                    itemNameLabel.AddToClassList("result-item-name");
                    itemRow.Add(itemNameLabel);

                    var selectBtn = new Button(() => SelectObject(item));
                    selectBtn.text = "选中";
                    selectBtn.AddToClassList("result-select-btn");
                    itemRow.Add(selectBtn);

                    foldout.Add(itemRow);
                }

                _resultScroll.Add(foldout);
            }
        }

        private void ScanAllAssets()
        {
            if (scanInProgress) return;

            scanInProgress = true;
            results.Clear();
            _scanButton.SetEnabled(false);
            try
            {
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                List<string> allAssets = new List<string>();
                allAssets.AddRange(sceneGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)));
                allAssets.AddRange(prefabGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)));

                int total = allAssets.Count;
                int current = 0;
                foreach (string assetPath in allAssets)
                {
                    current++;
                    if (EditorUtility.DisplayCancelableProgressBar("扫描丢失引用", $"正在处理: {Path.GetFileName(assetPath)} ({current}/{total})", (float)current / total))
                        break;

                    if (assetPath.EndsWith(".unity"))
                    {
                        if (IsReadOnlyScene(assetPath)) continue;
                        ScanScene(assetPath);
                    }
                    else if (assetPath.EndsWith(".prefab"))
                        ScanPrefab(assetPath);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                scanInProgress = false;
                _scanButton.SetEnabled(true);
                Repaint();
                RefreshResults();
            }
        }

        private bool IsReadOnlyScene(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath)) return false;
            return File.GetAttributes(fullPath).HasFlag(FileAttributes.ReadOnly);
        }

        private void ScanScene(string scenePath)
        {
            Scene scene = default;
            bool wasActive = false;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                if (!scene.IsValid()) return;
                wasActive = true;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                    ScanGameObject(root, root.name, scenePath, ResultType.Scene);
            }
            finally
            {
                if (wasActive && scene.IsValid())
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private void ScanPrefab(string prefabPath)
        {
            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabRoot == null) return;
                ScanGameObject(prefabRoot, prefabRoot.name, prefabPath, ResultType.Prefab);
            }
            finally
            {
                if (prefabRoot != null)
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private void ScanGameObject(GameObject go, string currentPath, string assetPath, ResultType type)
        {
            var missingTypes = GetMissingTypes(go);
            if (missingTypes.Count > 0)
            {
                foreach (var missingType in missingTypes)
                {
                    results.Add(new ResultItem
                    {
                        type = type,
                        assetPath = assetPath,
                        objectPath = currentPath,
                        missingType = missingType,
                        displayName = $"[{type}] {Path.GetFileName(assetPath)} -> {currentPath}"
                    });
                }
            }

            foreach (Transform child in go.transform)
                ScanGameObject(child.gameObject, currentPath + "/" + child.name, assetPath, type);
        }

        private List<MissingType> GetMissingTypes(GameObject go)
        {
            var missingTypes = new List<MissingType>();
            var components = go.GetComponents<Component>();

            bool hasMissingScript = false;
            foreach (var comp in components)
            {
                if (comp == null) { hasMissingScript = true; break; }
            }
            if (hasMissingScript) missingTypes.Add(MissingType.MissingScript);

            if (detectFieldMissing)
            {
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    if (HasMissingFieldInComponent(comp)) { missingTypes.Add(MissingType.MissingFieldReference); break; }
                }
            }
            return missingTypes;
        }

        private string GetMissingTypeName(MissingType missingType)
        {
            switch (missingType)
            {
                case MissingType.MissingScript: return "脚本缺失 (Missing Script)";
                case MissingType.MissingFieldReference: return "字段引用丢失 (Missing Reference)";
                default: return missingType.ToString();
            }
        }

        private bool HasMissingFieldInComponent(Component comp)
        {
            SerializedObject so = new SerializedObject(comp);
            var prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference
                    && prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                    return true;
            }
            return false;
        }

        private void SelectObject(ResultItem item)
        {
            if (item.type == ResultType.Scene) OpenSceneAndSelect(item.assetPath, item.objectPath);
            else if (item.type == ResultType.Prefab) OpenPrefabAndSelect(item.assetPath, item.objectPath);
        }

        private void OpenSceneAndSelect(string scenePath, string objectPath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.isLoaded)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            GameObject target = FindGameObjectByPath(objectPath);
            if (target != null)
            {
                Selection.activeGameObject = target;
                EditorGUIUtility.PingObject(target);
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            }
            else Debug.LogWarning($"无法找到物体: {objectPath}，可能路径已变化");
        }

        private void OpenPrefabAndSelect(string prefabPath, string objectPath)
        {
            PrefabStage stage = PrefabStageUtility.OpenPrefab(prefabPath);
            if (stage == null) return;
            GameObject root = stage.prefabContentsRoot;
            GameObject target = FindGameObjectByPathFromRoot(root, objectPath);
            if (target != null)
            {
                Selection.activeGameObject = target;
                EditorGUIUtility.PingObject(target);
            }
            else Debug.LogWarning($"无法在预制体中找到物体: {objectPath}");
        }

        private GameObject FindGameObjectByPath(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                foreach (var root in scene.GetRootGameObjects())
                {
                    var result = FindGameObjectByPathFromRoot(root, path);
                    if (result != null) return result;
                }
            }
            return null;
        }

        private GameObject FindGameObjectByPathFromRoot(GameObject root, string path)
        {
            if (root.name == path) return root;
            string[] parts = path.Split('/');
            if (parts.Length == 0 || root.name != parts[0]) return null;

            Transform current = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.Find(parts[i]);
                if (child == null) return null;
                current = child;
            }
            return current.gameObject;
        }

        private enum ResultType { Scene, Prefab }
        private enum MissingType { MissingScript, MissingFieldReference }

        private class ResultItem
        {
            public ResultType type;
            public MissingType missingType;
            public string assetPath;
            public string objectPath;
            public string displayName;
        }
    }
}
