#if UNITY_EDITOR
using CFramework;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor
{
    /// <summary>
    ///     音频调试窗口 —— 运行时查看和调节所有分组的音量/Slot/快照
    ///     <para>菜单：Tools > CFramework > Audio Debugger</para>
    /// </summary>
    public class AudioDebuggerWindow : EditorWindow
    {
        [MenuItem("Tools/CFramework/Audio Debugger")]
        private static void Open()
        {
            GetWindow<AudioDebuggerWindow>("Audio Debugger");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Audio Debugger 仅在运行时可用。", MessageType.Info);
                return;
            }

            // 尝试获取 AudioService 实例
            var audioService = GetAudioService();
            if (audioService == null)
            {
                EditorGUILayout.HelpBox("AudioService 未初始化。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Audio Groups", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // 显示所有分组
            foreach (var group in audioService.GetAllGroups())
            {
                var info = audioService.GetGroupInfo(group);
                EditorGUILayout.Space(4);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{group}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Path: {info.Path}");
                EditorGUILayout.LabelField($"  Slots: {info.ActiveSlots}/{info.TotalSlots} active");

                // 音量显示与调节
                var currentVol = info.Volume;
                var newVol = EditorGUILayout.Slider("  Volume", currentVol, 0f, 1f);
                if (UnityEngine.Mathf.Abs(newVol - currentVol) > 0.001f)
                    audioService.SetGroupVolume(group, newVol);

                // 静音按钮
                var isMuted = info.IsMuted;
                var newMuted = EditorGUILayout.Toggle("  Muted", isMuted);
                if (newMuted != isMuted)
                    audioService.MuteGroup(group, newMuted);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Snapshots", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // 显示快照按钮
            var currentSnapshot = audioService.CurrentSnapshot;
            EditorGUILayout.LabelField($"  Current: {currentSnapshot}");
            EditorGUILayout.Space(2);

            foreach (var name in audioService.GetSnapshotNames())
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(name, GUILayout.MinWidth(120)))
                    audioService.TransitionToSnapshotAsync(name, 0.5f).Forget();
                if (name == currentSnapshot)
                    EditorGUILayout.LabelField("◀", GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // 持久化按钮
            if (GUILayout.Button("Save Volumes", GUILayout.Height(30)))
                audioService.SaveVolumes();
        }

        /// <summary>
        ///     获取 AudioService 实例
        ///     <para>通过 VContainer 的 LifecycleContainer 查找</para>
        /// </summary>
        private IAudioService GetAudioService()
        {
            // 尝试通过 GameScope 获取
            var gameScope = Object.FindObjectOfType<GameScope>();
            if (gameScope != null)
            {
                // GameScope 通常通过 DI 容器管理，这里使用简单的服务定位
                // 实际项目中可能需要更优雅的方式
            }

            // 备用方案：查找所有已注册的 IAudioService
            // 这里使用 FindObjectOfType + GetComponent 方式简化
            return null; // 需要根据项目实际的 DI 获取方式调整
        }
    }
}
#endif
