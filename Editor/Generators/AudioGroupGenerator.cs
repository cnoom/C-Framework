#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using AudioUtility = UnityEditor.AudioUtil;

namespace CFramework.Editor
{
    /// <summary>
    ///     AudioGroup 枚举自动生成工具
    ///     <para>读取 AudioMixer 中的所有 Group 路径，自动生成哈希枚举</para>
    ///     <para>使用：菜单 CFramework > Generate > AudioGroup</para>
    /// </summary>
    public class AudioGroupGeneratorWindow : EditorWindow
    {
        private AudioMixer _mixer;
        private string _outputPath = "Assets/Scripts/Audio/AudioGroup.cs";
        private string _namespace = "CFramework";
        private string _enumName = "AudioGroup";
        private Vector2 _scroll;
        private List<string> _previewGroups = new();
        private bool _previewDirty = true;

        [MenuItem("CFramework/Generate/AudioGroup Enum", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioGroupGeneratorWindow>("AudioGroup 生成器");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            // 加载上次使用的 AudioMixer
            var lastPath = EditorPrefs.GetString("CFramework_AudioGroup_Mixer", "");
            if (!string.IsNullOrEmpty(lastPath))
                _mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(lastPath);

            _outputPath = EditorPrefs.GetString("CFramework_AudioGroup_Output", _outputPath);
            _namespace = EditorPrefs.GetString("CFramework_AudioGroup_Namespace", _namespace);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("AudioGroup 枚举自动生成", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "读取 AudioMixer 中的所有 Group 路径，自动计算 Animator.StringToHash 并生成 C# 枚举。\n" +
                "生成的枚举值与 AudioMixerTree.PathHash() 计算结果一致。",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("配置", EditorStyles.boldLabel);

            // 选择 AudioMixer
            EditorGUI.BeginChangeCheck();
            _mixer = (AudioMixer)EditorGUILayout.ObjectField("AudioMixer", _mixer, typeof(AudioMixer), false);
            if (EditorGUI.EndChangeCheck())
            {
                _previewDirty = true;
                if (_mixer != null)
                    EditorPrefs.SetString("CFramework_AudioGroup_Mixer", AssetDatabase.GetAssetPath(_mixer));
            }

            // 输出路径
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _outputPath = EditorGUILayout.TextField("输出路径", _outputPath);
            if (EditorGUILayout.Button("浏览...", GUILayout.Width(60)))
            {
                var selected = EditorUtility.SaveFilePanelInProject(
                    "选择输出路径",
                    "AudioGroup.cs",
                    "cs",
                    "选择 AudioGroup.cs 的输出路径");
                if (!string.IsNullOrEmpty(selected))
                {
                    _outputPath = selected;
                    _previewDirty = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString("CFramework_AudioGroup_Output", _outputPath);

            // 命名空间
            EditorGUI.BeginChangeCheck();
            _namespace = EditorGUILayout.TextField("命名空间", _namespace);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString("CFramework_AudioGroup_Namespace", _namespace);

            // 枚举名
            EditorGUI.BeginChangeCheck();
            _enumName = EditorGUILayout.TextField("枚举名", _enumName);
            if (EditorGUI.EndChangeCheck())
                _previewDirty = true;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);

            // 预览生成的 Group 列表
            if (_mixer != null)
            {
                if (_previewDirty)
                {
                    _previewGroups = CollectGroups(_mixer);
                    _previewDirty = false;
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.Height(160)))
                {
                    _scroll = scroll.scrollPosition;
                    foreach (var group in _previewGroups)
                    {
                        var hash = Animator.StringToHash(group);
                        EditorGUILayout.LabelField($"  {group}", $"= {hash}");
                    }
                }

                EditorGUILayout.HelpBox($"共 { _previewGroups.Count} 个 Group", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("请先指定 AudioMixer", MessageType.Warning);
            }

            EditorGUILayout.Space(8);

            // 生成按钮
            GUI.enabled = _mixer != null && !string.IsNullOrWhiteSpace(_outputPath);
            if (GUILayout.Button("生成 AudioGroup.cs", GUILayout.Height(30)))
            {
                Generate(_mixer, _outputPath, _namespace, _enumName);
            }
            GUI.enabled = true;

            EditorGUILayout.Space(4);
            if (GUILayout.Button("在文件夹中打开", GUILayout.Height(22)))
            {
                var dir = Path.GetDirectoryName(_outputPath);
                if (Directory.Exists(dir))
                    EditorUtility.RevealInFinder(_outputPath);
            }
        }

        /// <summary>
        ///     从 AudioMixer 收集所有 Group 路径
        /// </summary>
        private static List<string> CollectGroups(AudioMixer mixer)
        {
            var groups = new List<string>();

            // 使用反射读取 AudioMixer 的 _children 字段
            // 因为 AudioMixer.GetGroupCount / GetGroup 是 internal 的
            var mixerObj = new SerializedObject(mixer);
            var groupsProp = mixerObj.FindProperty("m_ChildPlugins");

            if (groupsProp != null && groupsProp.isArray)
            {
                for (int i = 0; i < groupsProp.arraySize; i++)
                {
                    var childProp = groupsProp.GetArrayElementAtIndex(i);
                    CollectChildGroups(childProp, "", groups);
                }
            }

            return groups;
        }

        private static void CollectChildGroups(SerializedProperty prop, string parentPath, List<string> groups)
        {
            // 读取 Group 的 name
            var nameProp = prop.FindPropertyRelative("Name");
            var name = nameProp?.stringValue ?? "";
            if (string.IsNullOrEmpty(name)) return;

            var fullPath = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
            groups.Add(fullPath);

            // 递归子 Group
            var childrenProp = prop.FindPropertyRelative("Children");
            if (childrenProp != null && childrenProp.isArray)
            {
                for (int i = 0; i < childrenProp.arraySize; i++)
                {
                    CollectChildGroups(childrenProp.GetArrayElementAtIndex(i), fullPath, groups);
                }
            }
        }

        /// <summary>
        ///     生成 AudioGroup.cs 枚举文件
        /// </summary>
        public static void Generate(AudioMixer mixer, string outputPath, string ns, string enumName)
        {
            var groups = CollectGroups(mixer);

            if (groups.Count == 0)
            {
                EditorUtility.DisplayDialog("AudioGroup 生成器", "未在 AudioMixer 中找到任何 Group", "确定");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("/// <auto-generated />");
            sb.AppendLine("/// 本文件由 AudioGroupGenerator 自动生成，请勿手动修改");
            sb.AppendLine($"/// 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"/// AudioMixer：{AssetDatabase.GetAssetPath(mixer)}");
            sb.AppendLine();
            sb.AppendLine("#if CFRAMEWORK_AUDIO");
            sb.AppendLine($"using System.ComponentModel;");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    ///     音频分组枚举");
            sb.AppendLine($"    ///     <para>值 = Animator.StringToHash(Group路径)，用于运行时 O(1) 查找</para>");
            sb.AppendLine($"    ///     <para>对应 AudioMixer 的 Group 层级</para>");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine($"    {{");

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var hash = Animator.StringToHash(group);
                // 枚举成员名：将 / 替换为 _，去除首尾空格
                var memberName = group.Replace("/", "_");
                // 添加 Description 属性用于调试
                sb.AppendLine($"        [Description(\"{group}\")]");
                sb.AppendLine($"        {memberName} = {hash}}{(i < groups.Count - 1 ? "," : "")}");
            }

            sb.AppendLine($"    }}");

            if (!string.IsNullOrWhiteSpace(ns))
                sb.AppendLine("}");

            sb.AppendLine("#endif");

            // 确保目录存在
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "生成成功",
                $"已生成 {groups.Count} 个枚举成员：\n{outputPath}",
                "确定");

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath));
        }
    }
}
#endif
