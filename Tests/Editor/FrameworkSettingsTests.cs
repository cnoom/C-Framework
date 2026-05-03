using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CFramework.EditorTests
{
    /// <summary>
    ///     编辑器工具测试
    /// </summary>
    [TestFixture]
    public class FrameworkSettingsTests
    {
        private FrameworkSettings _settings;
        private List<ScriptableObject> _cleanup;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<FrameworkSettings>();
            _cleanup = new List<ScriptableObject> { _settings };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _cleanup)
                if (obj != null) Object.DestroyImmediate(obj);
        }

        /// <summary>
        ///     辅助方法：为 FrameworkSettings 创建并挂载子 Settings
        /// </summary>
        private void AttachSubSettings()
        {
            _settings.Asset = ScriptableObject.CreateInstance<AssetSettings>();
            _settings.UI = ScriptableObject.CreateInstance<UISettings>();
            _settings.Audio = ScriptableObject.CreateInstance<AudioSettings>();
            _settings.Save = ScriptableObject.CreateInstance<SaveSettings>();
            _settings.Pool = ScriptableObject.CreateInstance<PoolSettings>();
            _settings.Log = ScriptableObject.CreateInstance<LogSettings>();
            _settings.Config = ScriptableObject.CreateInstance<ConfigSettings>();

            _cleanup.AddRange(new ScriptableObject[]
            {
                _settings.Asset, _settings.UI, _settings.Audio,
                _settings.Save, _settings.Pool, _settings.Log, _settings.Config
            });
        }

        [Test]
        public void FrameworkSettings_DefaultValues_AreValid()
        {
            // 子 Settings 为空时应通过 Get*Settings() fallback 到默认值
            var assetSettings = _settings.GetAssetSettings();
            var uiSettings = _settings.GetUISettings();
            var audioSettings = _settings.GetAudioSettings();

            Assert.Greater(assetSettings.MemoryBudgetMB, 0, "内存预算应大于0");
            Assert.Greater(assetSettings.MaxLoadPerFrame, 0, "每帧最大加载数应大于0");
            Assert.Greater(uiSettings.MaxNavigationStack, 0, "最大导航栈深度应大于0");
            Assert.Greater(audioSettings.MaxSlotsPerGroup, 0, "每组最大 Slot 数应大于0");
            Assert.IsFalse(string.IsNullOrEmpty(audioSettings.VolumePrefsPrefix), "音量存储前缀不应为空");
        }

        [Test]
        public void FrameworkSettings_CanCreateAsset()
        {
            // Arrange
            var path = "Assets/TestFrameworkSettings.asset";

            // Act
            AssetDatabase.CreateAsset(_settings, path);
            var loaded = AssetDatabase.LoadAssetAtPath<FrameworkSettings>(path);

            // Assert
            Assert.IsNotNull(loaded, "应能创建并加载设置资产");

            // Cleanup
            AssetDatabase.DeleteAsset(path);
        }

        [Test]
        public void FrameworkSettings_CanModifyValues()
        {
            // Arrange - 挂载子 Settings
            AttachSubSettings();

            // Act
            _settings.Asset.MemoryBudgetMB = 1024;
            _settings.Asset.MaxLoadPerFrame = 10;
            _settings.Audio.MaxSlotsPerGroup = 30;

            // Assert
            Assert.AreEqual(1024, _settings.Asset.MemoryBudgetMB);
            Assert.AreEqual(10, _settings.Asset.MaxLoadPerFrame);
            Assert.AreEqual(30, _settings.Audio.MaxSlotsPerGroup);
        }
    }
}