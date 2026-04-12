using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CFramework.Editor.Windows.Tools
{
    public class MissingReferenceFinder : EditorWindow
    {
        private List<ResultItem> results = new List<ResultItem>();
        private Vector2 scrollPos;
        private bool scanInProgress = false;
        private bool detectFieldMissing = true; // 是否检测字段引用丢失
        private Dictionary<MissingType, bool> foldoutStates = new Dictionary<MissingType, bool>
        {
            { MissingType.MissingScript, true },
            { MissingType.MissingFieldReference, true }
        };

        [MenuItem("CFramework/查找丢失引用物体")]
        public static void ShowWindow()
        {
            GetWindow<MissingReferenceFinder>("丢失引用查找器");
        }

        private void OnGUI()
        {
            GUILayout.Label("扫描丢失引用", EditorStyles.boldLabel);
            detectFieldMissing = EditorGUILayout.Toggle("检测字段引用丢失 (可能误报)", detectFieldMissing);
            if (GUILayout.Button("开始扫描", GUILayout.Height(30)))
            {
                if (!scanInProgress)
                    ScanAllAssets();
            }

            EditorGUILayout.Space();
            GUILayout.Label($"共找到 {results.Count} 个存在丢失引用的物体", EditorStyles.boldLabel);

            // 按缺失类型分组显示
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (MissingType missingType in System.Enum.GetValues(typeof(MissingType)))
            {
                var group = results.Where(r => r.missingType == missingType).ToList();
                if (group.Count == 0) continue;

                // 获取类型的中文显示名
                string typeName = GetMissingTypeName(missingType);
                if (!foldoutStates.ContainsKey(missingType))
                    foldoutStates[missingType] = true;

                foldoutStates[missingType] = EditorGUILayout.Foldout(foldoutStates[missingType], $"{typeName} ({group.Count})", true, EditorStyles.boldLabel);
                if (!foldoutStates[missingType]) continue;

                EditorGUI.indentLevel++;
                for (int i = 0; i < group.Count; i++)
                {
                    var item = group[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(item.displayName, GUILayout.MinWidth(300));
                    if (GUILayout.Button("选中", GUILayout.Width(60)))
                    {
                        SelectObject(item);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
        }

        private void ScanAllAssets()
        {
            scanInProgress = true;
            results.Clear();
            try
            {
                // 查找所有场景和预制体
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
                    {
                        break;
                    }

                    if (assetPath.EndsWith(".unity"))
                    {
                        // 跳过只读场景（如被版本控制锁定的场景）
                        if (IsReadOnlyScene(assetPath)) continue;
                        ScanScene(assetPath);
                    }
                    else if (assetPath.EndsWith(".prefab"))
                    {
                        ScanPrefab(assetPath);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                scanInProgress = false;
                Repaint();
            }
        }

        /// <summary>
        /// 判断场景文件是否为只读（被版本控制锁定或文件属性为只读）
        /// </summary>
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
                // 以只读模式临时打开场景 (Additive模式避免关闭当前场景)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                if (!scene.IsValid()) return;
                wasActive = true;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    ScanGameObject(root, root.name, scenePath, ResultType.Scene);
                }
            }
            finally
            {
                if (wasActive && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
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
            {
                ScanGameObject(child.gameObject, currentPath + "/" + child.name, assetPath, type);
            }
        }

        /// <summary>
        /// 获取 GameObject 上所有缺失类型
        /// </summary>
        private List<MissingType> GetMissingTypes(GameObject go)
        {
            var missingTypes = new List<MissingType>();
            var components = go.GetComponents<Component>();

            // 检测 Missing Script
            bool hasMissingScript = false;
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    hasMissingScript = true;
                    break;
                }
            }
            if (hasMissingScript)
                missingTypes.Add(MissingType.MissingScript);

            // 检测字段引用丢失（可选）
            if (detectFieldMissing)
            {
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    if (HasMissingFieldInComponent(comp))
                    {
                        missingTypes.Add(MissingType.MissingFieldReference);
                        break;
                    }
                }
            }
            return missingTypes;
        }

        /// <summary>
        /// 获取缺失类型的中文显示名称
        /// </summary>
        private string GetMissingTypeName(MissingType missingType)
        {
            switch (missingType)
            {
                case MissingType.MissingScript:
                    return "脚本缺失 (Missing Script)";
                case MissingType.MissingFieldReference:
                    return "字段引用丢失 (Missing Reference)";
                default:
                    return missingType.ToString();
            }
        }

        private bool HasMissingFieldInComponent(Component comp)
        {
            SerializedObject so = new SerializedObject(comp);
            var prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    // 如果引用为null但instanceID不为0，说明曾经引用过资源但已丢失
                    if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                    {
                        // 避免误报某些内置默认null字段（如Animator的avatar），但多数情况是需要关注的
                        // 简单排除特定属性名可减少误报，但为了完整性不做过滤
                        return true;
                    }
                }
            }
            return false;
        }

        private void SelectObject(ResultItem item)
        {
            if (item.type == ResultType.Scene)
            {
                OpenSceneAndSelect(item.assetPath, item.objectPath);
            }
            else if (item.type == ResultType.Prefab)
            {
                OpenPrefabAndSelect(item.assetPath, item.objectPath);
            }
        }

        private void OpenSceneAndSelect(string scenePath, string objectPath)
        {
            // 检查场景是否已打开
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.isLoaded)
            {
                // 保存当前场景（用户可选择取消）
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // 根据路径查找物体
            GameObject target = FindGameObjectByPath(objectPath);
            if (target != null)
            {
                Selection.activeGameObject = target;
                EditorGUIUtility.PingObject(target);
                // 在Scene视图中聚焦
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
            }
            else
            {
                Debug.LogWarning($"无法找到物体: {objectPath}，可能路径已变化");
            }
        }

        private void OpenPrefabAndSelect(string prefabPath, string objectPath)
        {
            // 打开预制体编辑模式
            PrefabStage stage = PrefabStageUtility.OpenPrefab(prefabPath);
            if (stage == null) return;

            // 在预制体根下查找物体
            GameObject root = stage.prefabContentsRoot;
            GameObject target = FindGameObjectByPathFromRoot(root, objectPath);
            if (target != null)
            {
                Selection.activeGameObject = target;
                EditorGUIUtility.PingObject(target);
            }
            else
            {
                Debug.LogWarning($"无法在预制体中找到物体: {objectPath}");
            }
        }

        private GameObject FindGameObjectByPath(string path)
        {
            // 在当前所有场景中查找（通常只有一个场景打开）
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                GameObject[] roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    GameObject result = FindGameObjectByPathFromRoot(root, path);
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

        private enum ResultType
        {
            Scene,
            Prefab
        }

        private enum MissingType
        {
            MissingScript,         // 脚本缺失
            MissingFieldReference  // 字段引用丢失
        }

        private class ResultItem
        {
            public ResultType type;
            public MissingType missingType; // 缺失类型
            public string assetPath;    // 场景或预制体路径
            public string objectPath;   // 物体在层次中的完整路径（从根开始）
            public string displayName;  // 显示用字符串
        }
    }
}