using System.IO;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Inspectors
{
    /// <summary>
    ///     FrameworkSettings 自定义 Inspector
    ///     <para>显示音频系统激活状态，引导用户生成 AudioGroup</para>
    /// </summary>
    [CustomEditor(typeof(FrameworkSettings))]
    internal sealed class FrameworkSettingsEditor : UnityEditor.Editor
    {
        private const string AudioGroupPath = "Assets/Scripts/Generated/AudioGroup.cs";

        public override void OnInspectorGUI()
        {
            // 音频系统状态提示
            DrawAudioStatus();

            EditorGUILayout.Space(4);

            // 默认 Inspector 绘制其余字段
            DrawDefaultInspector();
        }

        /// <summary>
        ///     绘制音频系统激活状态提示框
        /// </summary>
        private void DrawAudioStatus()
        {
            var audioEnabled = IsAudioSystemEnabled();

            if (audioEnabled)
            {
                EditorGUILayout.HelpBox(
                    "音频系统已激活 ✅\nAudioGroup 枚举已生成，音频服务将在运行时正常工作。",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "音频系统未激活 ⚠️\n尚未生成 AudioGroup 枚举，音频服务不可用。\n" +
                    "请通过菜单 Tools > CFramework > Generate AudioGroup Enum 生成。",
                    MessageType.Warning);

                if (GUILayout.Button("生成 AudioGroup 枚举", GUILayout.Height(28)))
                {
                    AudioGroupGenerator.Generate();
                }
            }
        }

        /// <summary>
        ///     检查音频系统是否已激活
        ///     <para>通过检查生成的 AudioGroup.cs 文件是否存在来判断</para>
        /// </summary>
        private static bool IsAudioSystemEnabled()
        {
            return File.Exists(AudioGroupPath);
        }

        /// <summary>
        ///     FrameworkSettings 创建工具（菜单入口）
        /// </summary>
        [MenuItem("CFramework/CreateSettings")]
        public static void CreateFrameworkSettings()
        {
            var settings = ScriptableObject.CreateInstance<FrameworkSettings>();

            var path = EditorUtility.SaveFilePanelInProject(
                "Save FrameworkSettings",
                "FrameworkSettings",
                "asset",
                "Save FrameworkSettings asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = settings;
            }
        }
    }
}
