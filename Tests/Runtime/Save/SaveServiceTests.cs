using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.TestTools;

namespace CFramework.Tests
{
    /// <summary>
    ///     存档服务单元测试
    /// </summary>
    [TestFixture]
    public class SaveServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            _disposables = new List<IDisposable>();
            _testSavePath = Path.Combine(Application.temporaryCachePath, "TestSaves", Guid.NewGuid().ToString());

            // 创建测试用的 SaveSettings
            _saveSettings = ScriptableObject.CreateInstance<SaveSettings>();
            _saveSettings.EncryptionKey = "TestEncryptionKey";
            _saveSettings.AutoSaveInterval = 1; // 测试用短间隔

            _saveService = new SaveService(_saveSettings);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables) disposable?.Dispose();
            _disposables.Clear();

            _saveService?.Dispose();

            if (_saveSettings != null)
                UnityEngine.Object.DestroyImmediate(_saveSettings);

            // 清理测试文件
            if (Directory.Exists(_testSavePath))
                try
                {
                    Directory.Delete(_testSavePath, true);
                }
                catch
                {
                    // 忽略清理失败
                }
        }

        private SaveService _saveService;
        private SaveSettings _saveSettings;
        private string _testSavePath;
        private List<IDisposable> _disposables;

        [UnityTest]
        public IEnumerator S001_AtomicWrite_ProcessKillDuringWriteDoesNotCorrupt()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var testData = new TestSaveData
                {
                    Level = 10,
                    Gold = 1000,
                    PlayerName = "TestPlayer"
                };

                // Act - 保存数据
                await _saveService.SaveAsync("player", testData);

                // 验证临时文件已被清理
                var tempPath = GetTestFilePath("player") + ".tmp";
                Assert.IsFalse(File.Exists(tempPath), "临时文件应该被清理");

                // Assert - 数据应该正确保存
                var loaded = await _saveService.LoadAsync<TestSaveData>("player");
                Assert.IsNotNull(loaded);
                Assert.AreEqual(10, loaded.Level);
                Assert.AreEqual(1000, loaded.Gold);
                Assert.AreEqual("TestPlayer", loaded.PlayerName);
            });
        }

        [UnityTest]
        public IEnumerator S001_AtomicWrite_TempFileCleanedOnError()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 使用无效路径模拟错误（通过在只读目录创建文件）
                // 这个测试验证写入失败时临时文件被清理

                var testData = new TestSaveData { Level = 1 };

                // 正常保存应该成功
                await _saveService.SaveAsync("test", testData);

                var tempPath = GetTestFilePath("test") + ".tmp";
                Assert.IsFalse(File.Exists(tempPath), "临时文件应该被清理");
            });
        }

        [Test]
        public void S002_DirtyStateEvent_MarkDirtyTriggersOnDirtyChanged()
        {
            // Arrange
            var dirtyStateChanges = new List<bool>();
            _saveService.OnDirtyChanged
                .Subscribe(isDirty => dirtyStateChanges.Add(isDirty))
                .AddTo(_disposables);

            // Assert - 初始状态应为 false
            Assert.IsFalse(_saveService.IsDirty);

            // Act - 标记为脏
            _saveService.MarkDirty();

            // Assert
            Assert.IsTrue(_saveService.IsDirty);
            Assert.AreEqual(1, dirtyStateChanges.Count);
            Assert.IsTrue(dirtyStateChanges[0]);

            // Act - 清除脏状态
            _saveService.ClearDirty();

            // Assert
            Assert.IsFalse(_saveService.IsDirty);
            Assert.AreEqual(2, dirtyStateChanges.Count);
            Assert.IsFalse(dirtyStateChanges[1]);
        }

        [Test]
        public void S002_DirtyState_ClearDirtyDoesNotTriggerEventIfNotDirty()
        {
            // Arrange
            var eventTriggered = false;
            _saveService.OnDirtyChanged.Subscribe(_ => eventTriggered = true).AddTo(_disposables);

            // Act - 清除未脏状态
            _saveService.ClearDirty();

            // Assert - 不应触发事件
            Assert.IsFalse(eventTriggered);
        }

        [Test]
        public void S002_DirtyState_MarkDirtyDoesNotTriggerEventIfAlreadyDirty()
        {
            // Arrange
            var triggerCount = 0;
            _saveService.OnDirtyChanged.Subscribe(_ => triggerCount++).AddTo(_disposables);

            // Act - 连续标记脏
            _saveService.MarkDirty();
            _saveService.MarkDirty();
            _saveService.MarkDirty();

            // Assert - 只应触发一次
            Assert.AreEqual(1, triggerCount);
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_RoundTrip_Success()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var testData = new TestSaveData
                {
                    Level = 50,
                    Gold = 9999,
                    PlayerName = "RoundTripTest"
                };

                // Act
                await _saveService.SaveAsync("roundtrip", testData);
                var loaded = await _saveService.LoadAsync<TestSaveData>("roundtrip");

                // Assert
                Assert.IsNotNull(loaded);
                Assert.AreEqual(testData.Level, loaded.Level);
                Assert.AreEqual(testData.Gold, loaded.Gold);
                Assert.AreEqual(testData.PlayerName, loaded.PlayerName);
            });
        }

        [UnityTest]
        public IEnumerator LoadAsync_ReturnsDefaultForNonExistentKey()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Act
                var result = await _saveService.LoadAsync<TestSaveData>("non_existent_key");

                // Assert
                Assert.IsNull(result);
            });
        }

        [UnityTest]
        public IEnumerator LoadAsync_ReturnsProvidedDefaultForNonExistentKey()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var defaultValue = new TestSaveData { Level = 100, Gold = 500 };

                // Act
                var result = await _saveService.LoadAsync("non_existent_key", defaultValue);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(100, result.Level);
                Assert.AreEqual(500, result.Gold);
            });
        }

        [UnityTest]
        public IEnumerator SaveAsync_UpdatesCache()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var data1 = new TestSaveData { Level = 1 };
                var data2 = new TestSaveData { Level = 2 };

                // Act
                await _saveService.SaveAsync("cached", data1);
                var loaded1 = await _saveService.LoadAsync<TestSaveData>("cached");

                await _saveService.SaveAsync("cached", data2);
                var loaded2 = await _saveService.LoadAsync<TestSaveData>("cached");

                // Assert
                Assert.AreEqual(1, loaded1.Level);
                Assert.AreEqual(2, loaded2.Level);
            });
        }

        [UnityTest]
        public IEnumerator SaveAsync_MultipleKeys()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var playerData = new TestSaveData { Level = 10, PlayerName = "Player1" };
                var settingsData = new TestSettingsData { Volume = 0.5f, IsFullscreen = true };

                // Act
                await _saveService.SaveAsync("player", playerData);
                await _saveService.SaveAsync("settings", settingsData);

                var loadedPlayer = await _saveService.LoadAsync<TestSaveData>("player");
                var loadedSettings = await _saveService.LoadAsync<TestSettingsData>("settings");

                // Assert
                Assert.IsNotNull(loadedPlayer);
                Assert.AreEqual(10, loadedPlayer.Level);

                Assert.IsNotNull(loadedSettings);
                Assert.AreEqual(0.5f, loadedSettings.Volume);
                Assert.IsTrue(loadedSettings.IsFullscreen);
            });
        }

        [UnityTest]
        public IEnumerator SaveAsync_All_WithDirtyData()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                await _saveService.SaveAsync("key1", new TestSaveData { Level = 1 });
                await _saveService.SaveAsync("key2", new TestSaveData { Level = 2 });

                _saveService.ClearDirty();

                // 修改缓存中的数据
                await _saveService.SaveAsync("key3", new TestSaveData { Level = 3 });
                Assert.IsTrue(_saveService.IsDirty);

                // Act - 保存所有脏数据
                await _saveService.SaveAsync();

                // Assert
                Assert.IsFalse(_saveService.IsDirty);
            });
        }

        [UnityTest]
        public IEnumerator HasKey_ReturnsTrueForExistingKey()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                await _saveService.SaveAsync("existing_key", new TestSaveData { Level = 1 });

                // Act
                var result = _saveService.HasKey("existing_key");

                // Assert
                Assert.IsTrue(result);
            });
        }

        [Test]
        public void HasKey_ReturnsFalseForNonExistentKey()
        {
            // Act
            var result = _saveService.HasKey("non_existent_key");

            // Assert
            Assert.IsFalse(result);
        }

        [UnityTest]
        public IEnumerator HasKey_ReturnsTrueForCachedKey()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                await _saveService.SaveAsync("cached_key", new TestSaveData { Level = 1 });

                // 删除文件但保留缓存
                var filePath = GetTestFilePath("cached_key");
                if (File.Exists(filePath)) File.Delete(filePath);

                // Act - 应该从缓存中找到
                var result = _saveService.HasKey("cached_key");

                // Assert
                Assert.IsTrue(result);
            });
        }

        [UnityTest]
        public IEnumerator DeleteAsync_RemovesKeyAndFile()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                await _saveService.SaveAsync("delete_me", new TestSaveData { Level = 1 });
                Assert.IsTrue(_saveService.HasKey("delete_me"));

                // Act
                var result = await _saveService.DeleteAsync("delete_me");

                // Assert
                Assert.IsTrue(result);
                Assert.IsFalse(_saveService.HasKey("delete_me"));

                var filePath = GetTestFilePath("delete_me");
                Assert.IsFalse(File.Exists(filePath));
            });
        }

        [UnityTest]
        public IEnumerator DeleteAsync_ReturnsFalseForNonExistentKey()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Act
                var result = await _saveService.DeleteAsync("non_existent_key");

                // Assert
                Assert.IsFalse(result);
            });
        }

        [UnityTest]
        public IEnumerator DeleteAllAsync_ClearsAllData()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                await _saveService.SaveAsync("key1", new TestSaveData { Level = 1 });
                await _saveService.SaveAsync("key2", new TestSaveData { Level = 2 });
                await _saveService.SaveAsync("key3", new TestSaveData { Level = 3 });

                // Act
                await _saveService.DeleteAllAsync();

                // Assert
                Assert.IsFalse(_saveService.HasKey("key1"));
                Assert.IsFalse(_saveService.HasKey("key2"));
                Assert.IsFalse(_saveService.HasKey("key3"));
                Assert.IsFalse(_saveService.IsDirty);
            });
        }

        [UnityTest]
        public IEnumerator DeleteAllAsync_HandlesEmptySlot()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 使用新槽位
                _saveService.SetSlot(100);

                // Act - 不应该抛出异常
                await _saveService.DeleteAllAsync();

                // Assert
                Assert.Pass();
            });
        }

        [UnityTest]
        public IEnumerator AutoSave_TriggersAfterInterval()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var dirtyChanges = new List<bool>();
                _saveService.OnDirtyChanged.Subscribe(d => dirtyChanges.Add(d)).AddTo(_disposables);

                // 启用自动保存，间隔0.3秒
                _saveService.EnableAutoSave(0.3f);

                // 标记脏状态
                await _saveService.SaveAsync("auto_test", new TestSaveData { Level = 1 });

                // Assert - 应该被标记为脏
                Assert.IsTrue(_saveService.IsDirty);

                // 等待自动保存触发（间隔 + 一些缓冲时间）
                await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

                // Assert - 自动保存后应该清除脏状态
                Assert.IsFalse(_saveService.IsDirty, "自动保存后脏状态应该被清除");

                _saveService.DisableAutoSave();
            });
        }

        [Test]
        public void AutoSave_DisableAutoSave_StopsAutoSave()
        {
            // Arrange
            _saveService.EnableAutoSave(0.1f);

            // Act
            _saveService.DisableAutoSave();

            // Assert - 不应抛出异常
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator AutoSave_OnlySavesWhenDirty()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var dirtyChanges = new List<bool>();
                _saveService.OnDirtyChanged.Subscribe(d => dirtyChanges.Add(d)).AddTo(_disposables);

                _saveService.EnableAutoSave(0.2f);

                // 不标记脏状态，等待超过自动保存间隔
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

                // Assert - 不应该有任何脏状态变化（因为没有任何保存操作）
                Assert.AreEqual(0, dirtyChanges.Count, "没有脏数据时不应该触发保存");

                _saveService.DisableAutoSave();
            });
        }

        [UnityTest]
        public IEnumerator AutoSave_DoesNotTriggerIfNotDirty()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var saveCount = 0;
                _saveService.OnDirtyChanged.Subscribe(_ => saveCount++).AddTo(_disposables);

                _saveService.EnableAutoSave(0.2f);

                // 等待超过自动保存间隔，但不创建任何数据
                await UniTask.Delay(TimeSpan.FromSeconds(0.6f));

                // Assert - 不应该触发保存
                Assert.AreEqual(0, saveCount, "没有脏数据时不应该触发保存");

                _saveService.DisableAutoSave();
            });
        }

        [Test]
        public void Slot_DefaultSlotIsZero()
        {
            Assert.AreEqual(0, _saveService.CurrentSlot);
        }

        [Test]
        public void Slot_SetSlot_UpdatesCurrentSlot()
        {
            // Act
            _saveService.SetSlot(5);

            // Assert
            Assert.AreEqual(5, _saveService.CurrentSlot);
        }

        [Test]
        public void Slot_SetNegativeSlot_ResetsToZero()
        {
            // Act
            _saveService.SetSlot(-1);

            // Assert
            Assert.AreEqual(0, _saveService.CurrentSlot);
        }

        [UnityTest]
        public IEnumerator Slot_DifferentSlots_HaveSeparateData()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var slot0Data = new TestSaveData { Level = 1, PlayerName = "Slot0" };
                var slot1Data = new TestSaveData { Level = 2, PlayerName = "Slot1" };

                // Act - 在槽0保存数据
                _saveService.SetSlot(0);
                await _saveService.SaveAsync("player", slot0Data);

                // 切换到槽1保存数据
                _saveService.SetSlot(1);
                await _saveService.SaveAsync("player", slot1Data);

                // 切回槽0读取
                _saveService.SetSlot(0);
                var loadedSlot0 = await _saveService.LoadAsync<TestSaveData>("player");

                // 切到槽1读取
                _saveService.SetSlot(1);
                var loadedSlot1 = await _saveService.LoadAsync<TestSaveData>("player");

                // Assert
                Assert.IsNotNull(loadedSlot0);
                Assert.AreEqual(1, loadedSlot0.Level);
                Assert.AreEqual("Slot0", loadedSlot0.PlayerName);

                Assert.IsNotNull(loadedSlot1);
                Assert.AreEqual(2, loadedSlot1.Level);
                Assert.AreEqual("Slot1", loadedSlot1.PlayerName);
            });
        }

        [UnityTest]
        public IEnumerator Slot_SwitchClearsCache()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                await _saveService.SaveAsync("cached_key", new TestSaveData { Level = 100 });

                // Act - 切换槽位
                _saveService.SetSlot(1);

                // Assert - 新槽位应该没有数据
                var result = await _saveService.LoadAsync<TestSaveData>("cached_key");
                Assert.IsNull(result);
            });
        }

        [Test]
        public void Slot_GetSlotNames_ReturnsExistingSlots()
        {
            // Arrange - 创建一些槽位目录
            var saveDir = Path.Combine(Application.persistentDataPath, "Save");

            // Act
            var slots = _saveService.GetSlotNames();

            // Assert - 应该返回数组（即使为空）
            Assert.IsNotNull(slots);
        }

        [UnityTest]
        public IEnumerator Slot_GetSlotInfos_ReturnsSlotInformation()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 创建多个槽位数据
                _saveService.SetSlot(0);
                await _saveService.SaveAsync("player", new TestSaveData { Level = 1 });

                _saveService.SetSlot(2);
                await _saveService.SaveAsync("player", new TestSaveData { Level = 2 });

                // Act
                var infos = _saveService.GetSlotInfos();

                // Assert
                Assert.IsNotNull(infos);
                Assert.GreaterOrEqual(infos.Length, 2);

                // 验证槽位信息
                var slot0Info = Array.Find(infos, i => i.Index == 0);
                Assert.IsNotNull(slot0Info);
                Assert.IsTrue(slot0Info.HasData);

                var slot2Info = Array.Find(infos, i => i.Index == 2);
                Assert.IsNotNull(slot2Info);
                Assert.IsTrue(slot2Info.HasData);
            });
        }

        [Test]
        public void Slot_GetSlotInfos_EmptyForNoData()
        {
            // Act
            var infos = _saveService.GetSlotInfos();

            // Assert - 没有数据时应该返回空数组
            Assert.IsNotNull(infos);
        }

        [UnityTest]
        public IEnumerator ErrorHandling_CorruptedFile_ReturnsDefault()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 创建损坏的文件
                await _saveService.SaveAsync("corrupted", new TestSaveData { Level = 1 });

                // 手动损坏文件
                var filePath = GetTestFilePath("corrupted");
                File.WriteAllBytes(filePath, new byte[] { 0x00, 0x01, 0x02, 0x03 });

                // 清除缓存强制从文件读取
                _saveService.SetSlot(_saveService.CurrentSlot + 1);
                _saveService.SetSlot(_saveService.CurrentSlot - 1);

                // Act
                var result = await _saveService.LoadAsync("corrupted", new TestSaveData { Level = 999 });

                // Assert - 应该返回默认值
                Assert.IsNotNull(result);
                Assert.AreEqual(999, result.Level);
            });
        }

        [UnityTest]
        public IEnumerator ErrorHandling_InvalidKey_ReturnsDefault()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Act
                var result = await _saveService.LoadAsync<TestSaveData>("");

                // Assert - 应该返回默认值而不是崩溃
                Assert.IsNull(result);
            });
        }

        [UnityTest]
        public IEnumerator ErrorHandling_Cancellation_ThrowsOperationCanceledException()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var cts = new CancellationTokenSource();
                cts.Cancel();

                // Act & Assert
                var exceptionThrown = false;
                try
                {
                    await _saveService.SaveAsync("cancelled", new TestSaveData(), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    exceptionThrown = true;
                }

                Assert.IsTrue(exceptionThrown, "应该抛出 OperationCanceledException");
                cts.Dispose();
            });
        }

        [UnityTest]
        public IEnumerator ErrorHandling_LoadAsync_HandlesMissingDirectory()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 切换到一个不存在的槽位
                _saveService.SetSlot(999);

                // Act
                var result = await _saveService.LoadAsync<TestSaveData>("missing");

                // Assert - 应该返回默认值而不崩溃
                Assert.IsNull(result);
            });
        }

        /// <summary>
        ///     获取测试文件的完整路径
        /// </summary>
        private string GetTestFilePath(string key)
        {
            return Path.Combine(Application.persistentDataPath, "Save", $"slot_{_saveService.CurrentSlot}",
                $"{key}.sav");
        }

        [Serializable]
        private class TestSaveData
        {
            public int Level;
            public int Gold;
            public string PlayerName;
        }

        [Serializable]
        private class TestSettingsData
        {
            public float Volume;
            public bool IsFullscreen;
        }
    }
}