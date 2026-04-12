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
            Assert.GreaterOrEqual(_settings.DefaultBGMVolume, 0f, "BGM 音量应大于等于0");
            Assert.LessOrEqual(_settings.DefaultBGMVolume, 1f, "BGM 音量应小于等于1");
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
            _settings.DefaultBGMVolume = 0.8f;

            // Assert
            Assert.AreEqual(1024, _settings.MemoryBudgetMB);
            Assert.AreEqual(10, _settings.MaxLoadPerFrame);
            Assert.AreEqual(0.8f, _settings.DefaultBGMVolume);
        }
    }
}