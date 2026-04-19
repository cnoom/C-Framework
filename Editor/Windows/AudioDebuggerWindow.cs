#if UNITY_EDITOR && CFRAMEWORK_AUDIO
using System.Collections.Generic;
using CFramework;
using CFramework.Editor.Utilities;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor
{
    /// <summary>
    ///     音频调试窗口 —— 运行时查看和调节所有分组的音量/Slot/快照（UIToolkit 实现）
    /// </summary>
    public class AudioDebuggerWindow : EditorWindow
    {
        private const string USS_FILE_NAME = "AudioDebuggerWindow.uss";

        [MenuItem("Tools/CFramework/Audio Debugger")]
        private static void Open()
        {
            var window = GetWindow<AudioDebuggerWindow>("Audio Debugger");
            window.minSize = new Vector2(350, 500);
        }

        private VisualElement _rootContainer;
        private Label _statusLabel;
        private ScrollView _groupScrollView;
        private ScrollView _snapshotScrollView;
        private Button _saveButton;

        private IAudioService _cachedAudioService;

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 加载 USS 样式表
            var styleSheet = EditorStyleSheet.Find(USS_FILE_NAME);
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            // 状态提示标签
            _statusLabel = new Label { text = "" };
            _statusLabel.AddToClassList("status-label");
            root.Add(_statusLabel);

            // 主容器
            _rootContainer = new VisualElement();
            _rootContainer.AddToClassList("main-container");

            // Audio Groups 区域
            var groupSection = new Label("Audio Groups");
            groupSection.AddToClassList("section-label");
            _rootContainer.Add(groupSection);

            _groupScrollView = new ScrollView();
            _groupScrollView.AddToClassList("group-scroll");
            _rootContainer.Add(_groupScrollView);

            // Snapshots 区域
            var snapshotSection = new Label("Snapshots");
            snapshotSection.AddToClassList("section-label");
            _rootContainer.Add(snapshotSection);

            _snapshotScrollView = new ScrollView();
            _snapshotScrollView.AddToClassList("snapshot-scroll");
            _rootContainer.Add(_snapshotScrollView);

            // 保存按钮
            _saveButton = new Button(OnSaveClicked) { text = "Save Volumes" };
            _saveButton.AddToClassList("save-button");
            _rootContainer.Add(_saveButton);

            root.Add(_rootContainer);
        }

        private void Update()
        {
            if (_rootContainer == null) return;

            if (!Application.isPlaying)
            {
                _statusLabel.text = "Audio Debugger 仅在运行时可用。";
                _statusLabel.style.display = DisplayStyle.Flex;
                _rootContainer.style.display = DisplayStyle.None;
                return;
            }

            var audioService = GetAudioService();
            if (audioService == null)
            {
                _statusLabel.text = "AudioService 未初始化。";
                _statusLabel.style.display = DisplayStyle.Flex;
                _rootContainer.style.display = DisplayStyle.None;
                return;
            }

            _cachedAudioService = audioService;
            _statusLabel.style.display = DisplayStyle.None;
            _rootContainer.style.display = DisplayStyle.Flex;

            RefreshUI(audioService);
        }

        private void RefreshUI(IAudioService audioService)
        {
            _groupScrollView.Clear();

            foreach (var group in audioService.GetAllGroupPaths())
            {
                var info = audioService.GetGroupInfo(group);
                _groupScrollView.Add(CreateGroupItem(group, info, audioService));
            }

            _snapshotScrollView.Clear();
            var currentSnapshot = audioService.CurrentSnapshot;

            var currentLabel = new Label($"  Current: {currentSnapshot}");
            currentLabel.AddToClassList("snapshot-current-label");
            _snapshotScrollView.Add(currentLabel);

            _snapshotScrollView.Add(new VisualElement { style = { height = 8 } });

            foreach (var name in audioService.GetSnapshotNames())
            {
                var isCurrent = name == currentSnapshot;
                var row = new VisualElement();
                row.AddToClassList("snapshot-row");

                var btn = new Button(() => audioService.TransitionToSnapshotAsync(name, 0.5f).Forget());
                btn.text = name;
                btn.AddToClassList("snapshot-btn");
                row.Add(btn);

                if (isCurrent)
                {
                    var indicator = new Label("◀");
                    indicator.AddToClassList("snapshot-indicator");
                    row.Add(indicator);
                }

                _snapshotScrollView.Add(row);
            }
        }

        private static VisualElement CreateGroupItem(string group, AudioGroupInfo info, IAudioService audioService)
        {
            var container = new VisualElement();
            container.AddToClassList("group-item");

            var nameLabel = new Label(group);
            nameLabel.AddToClassList("group-name");
            container.Add(nameLabel);

            var pathLabel = new Label($"  Path: {info.Path}");
            pathLabel.AddToClassList("group-detail");
            container.Add(pathLabel);

            var slotLabel = new Label($"  Slots: {info.ActiveSlots}/{info.TotalSlots} active");
            slotLabel.AddToClassList("group-detail");
            container.Add(slotLabel);

            var sliderRow = new VisualElement();
            sliderRow.AddToClassList("slider-row");

            var volSlider = new Slider(0f, 1f)
            {
                value = info.Volume,
                label = "Volume"
            };
            volSlider.style.width = Length.Percent(100);
            volSlider.RegisterValueChangedCallback(evt =>
            {
                audioService.SetGroupVolume(group, evt.newValue);
            });
            sliderRow.Add(volSlider);
            container.Add(sliderRow);

            var muteToggle = new Toggle("Muted")
            {
                value = info.IsMuted
            };
            muteToggle.AddToClassList("mute-toggle");
            muteToggle.RegisterValueChangedCallback(evt =>
            {
                audioService.MuteGroup(group, evt.newValue);
            });
            container.Add(muteToggle);

            return container;
        }

        private void OnSaveClicked()
        {
            _cachedAudioService?.SaveVolumes();
        }

        /// <summary>
        ///     获取 AudioService 实例
        /// </summary>
        private IAudioService GetAudioService()
        {
            var gameScope = UnityEngine.Object.FindObjectOfType<GameScope>();
            if (gameScope == null) return null;
            var components = gameScope.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp is IAudioService audioService)
                    return audioService;
            }
            return null;
        }
    }
}
#endif
