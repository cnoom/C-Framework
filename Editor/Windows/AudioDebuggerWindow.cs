#if UNITY_EDITOR && CFRAMEWORK_AUDIO
using System.Collections.Generic;
using CFramework;
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

            // 状态提示标签
            _statusLabel = new Label { text = "" };
            _statusLabel.style.fontSize = 13;
            _statusLabel.style.color = new Color(0.7f, 0.7f, 0.3f);
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(_statusLabel);

            // 主容器
            _rootContainer = new VisualElement();
            _rootContainer.style.flexDirection = FlexDirection.Column;
            _rootContainer.style.flexGrow = 1;

            // Audio Groups 区域
            var groupSection = new Label("Audio Groups");
            groupSection.style.fontSize = 13;
            groupSection.style.color = new Color(0.78f, 0.78f, 0.78f);
            groupSection.style.marginTop = 10;
            _rootContainer.Add(groupSection);

            _groupScrollView = new ScrollView();
            _groupScrollView.style.flexGrow = 1;
            _groupScrollView.style.paddingTop = 4;
            _groupScrollView.style.paddingBottom = 4;
            _rootContainer.Add(_groupScrollView);

            // Snapshots 区域
            var snapshotSection = new Label("Snapshots");
            snapshotSection.style.fontSize = 13;
            snapshotSection.style.color = new Color(0.78f, 0.78f, 0.78f);
            snapshotSection.style.marginTop = 10;
            _rootContainer.Add(snapshotSection);

            _snapshotScrollView = new ScrollView();
            _snapshotScrollView.style.flexGrow = 1;
            _rootContainer.Add(_snapshotScrollView);

            // 保存按钮
            _saveButton = new Button(OnSaveClicked) { text = "Save Volumes" };
            _saveButton.style.height = 30;
            _saveButton.style.marginTop = 10;
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
            currentLabel.style.fontSize = 11;
            currentLabel.style.color = new Color(0.63f, 0.79f, 0.63f);
            currentLabel.style.marginLeft = 8;
            _snapshotScrollView.Add(currentLabel);

            _snapshotScrollView.Add(new VisualElement { style = { height = 8 } });

            foreach (var name in audioService.GetSnapshotNames())
            {
                var isCurrent = name == currentSnapshot;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 2;

                var btn = new Button(() => audioService.TransitionToSnapshotAsync(name, 0.5f).Forget());
                btn.text = name;
                btn.style.minWidth = 120;
                btn.style.fontSize = 11;
                row.Add(btn);

                if (isCurrent)
                {
                    var indicator = new Label("◀");
                    indicator.style.color = new Color(0.39f, 0.71f, 1f);
                    indicator.style.marginLeft = 4;
                    row.Add(indicator);
                }

                _snapshotScrollView.Add(row);
            }
        }

        private static VisualElement CreateGroupItem(string group, AudioGroupInfo info, IAudioService audioService)
        {
            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.7f);
            container.style.paddingTop = 6;
            container.style.paddingBottom = 6;
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            container.style.marginBottom = 6;

            var nameLabel = new Label(group);
            nameLabel.style.fontSize = 12;
            nameLabel.style.color = new Color(0.86f, 0.86f, 0.86f);
            container.Add(nameLabel);

            var pathLabel = new Label($"  Path: {info.Path}");
            pathLabel.style.fontSize = 11;
            pathLabel.style.color = new Color(0.59f, 0.59f, 0.59f);
            pathLabel.style.marginLeft = 12;
            container.Add(pathLabel);

            var slotLabel = new Label($"  Slots: {info.ActiveSlots}/{info.TotalSlots} active");
            slotLabel.style.fontSize = 11;
            slotLabel.style.color = new Color(0.59f, 0.59f, 0.59f);
            slotLabel.style.marginLeft = 12;
            container.Add(slotLabel);

            var sliderRow = new VisualElement();
            sliderRow.style.marginLeft = 16;
            sliderRow.style.marginTop = 4;

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
            muteToggle.style.marginLeft = 16;
            muteToggle.style.marginTop = 2;
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
            // 尝试从 GameScope 获取 IAudioService
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
