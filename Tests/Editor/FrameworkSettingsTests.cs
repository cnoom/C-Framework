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
        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<FrameworkSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null) Object.DestroyImmediate(_settings);
        }

        private FrameworkSettings _settings;

        [Test]
        public void FrameworkSettings_DefaultValues_AreValid()
        {
            // Assert - 验证默认值合理性
            Assert.Greater(_settings.MemoryBudgetMB, 0, "内存预算应大于0");
            Assert.Greater(_settings.MaxLoadPerFrame, 0, "每帧最大加载数应大于0");
            Assert.Greater(_settings.MaxNavigationStack, 0, "最大导航栈深度应大于0");
            Assert.Greater(_settings.MaxSlotsPerGroup, 0, "每组最大 Slot 数应大于0");
            Assert.IsFalse(string.IsNullOrEmpty(_settings.VolumePrefsPrefix), "音量存储前缀不应为空");
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
            // Act
            _settings.MemoryBudgetMB = 1024;
            _settings.MaxLoadPerFrame = 10;
            _settings.MaxSlotsPerGroup = 30;

            // Assert
            Assert.AreEqual(1024, _settings.MemoryBudgetMB);
            Assert.AreEqual(10, _settings.MaxLoadPerFrame);
            Assert.AreEqual(30, _settings.MaxSlotsPerGroup);
        }
    }
}