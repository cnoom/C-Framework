#if UNITY_EDITOR && CFRAMEWORK_AUDIO
using System.Collections.Generic;
using CFramework;
using CNoom.UnityTool.Editor;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFramework.Editor.Windows
{
    /// <summary>
    ///     Dashboard 音频调试 Tab 的内容构建器
    ///     从 AudioDebuggerWindow 提取，嵌入 Dashboard 使用
    ///     仅在 Play Mode + AudioService 可用时激活内容区域
    /// </summary>
    public class DashboardAudioDebuggerTab
    {
        #region 控件引用

        private VisualElement _rootContainer;
        private Label _statusLabel;
        private ScrollView _groupScrollView;
        private ScrollView _snapshotScrollView;
        private Button _saveButton;

        private IAudioService _cachedAudioService;

        #endregion

        #region 公开接口

        /// <summary>
        ///     创建 Tab 内容
        /// </summary>
        public VisualElement CreateContent()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            // 加载 USS
            var styleSheet = EditorStyleSheet.Find("AudioDebuggerWindow.uss");
            if (styleSheet != null) container.styleSheets.Add(styleSheet);

            // 状态提示标签（非运行时 / 服务不可用时显示）
            _statusLabel = new Label { text = "Audio Debugger 仅在运行时可用。" };
            _statusLabel.AddToClassList("runtime-status-label");
            container.Add(_statusLabel);

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

            container.Add(_rootContainer);

            // 默认隐藏主内容
            _rootContainer.style.display = DisplayStyle.None;

            return container;
        }

        /// <summary>
        ///     每帧更新（由 Dashboard 的 Update 调用）
        /// </summary>
        public void Update()
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

        #endregion

        #region UI 刷新

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

        #endregion

        #region 服务获取

        private static IAudioService GetAudioService()
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

        #endregion
    }
}
#endif
