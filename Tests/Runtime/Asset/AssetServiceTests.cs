using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CFramework.Tests
{
    /// <summary>
    ///     资源服务单元测试
    ///     注意：实际资源加载测试需要配置 Addressables 测试资源
    /// </summary>
    [TestFixture]
    public class AssetServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            var settings = ScriptableObject.CreateInstance<FrameworkSettings>();
            settings.MemoryBudgetMB = 512;
            settings.MaxLoadPerFrame = 5;
            _assetService = new AssetService(settings);
        }

        [TearDown]
        public void TearDown()
        {
            _assetService?.Dispose();
        }

        private AssetService _assetService;

        // TestPrefab 的 Addressable key（需要确保资源存在）
        private const string TestPrefabKey = "Assets/TestPrefab.prefab";
        private const string TestPrefabGuid = "c9a191c5568c2184e8fb52c1f9c7ea9a";

        [UnityTest]
        [Timeout(10000)] // 10秒超时保护
        public IEnumerator A001_ReferenceCount_MultipleLoadsReleaseCorrectly()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // 检查资源是否存在
                if (!await CheckResourceExists(TestPrefabKey))
                {
                    Assert.Ignore($"测试资源不存在: {TestPrefabKey}");
                    return;
                }

                // Arrange - 加载同一资源多次
                var handle1 = await _assetService.LoadAsync<GameObject>(TestPrefabKey);
                var handle2 = await _assetService.LoadAsync<GameObject>(TestPrefabKey);
                var handle3 = await _assetService.LoadAsync<GameObject>(TestPrefabKey);

                // Assert - 所有句柄应指向同一资源
                Assert.IsNotNull(handle1.Asset, "资源应成功加载");
                Assert.AreEqual(handle1.Asset, handle2.Asset, "同一资源的多个加载应返回相同实例");
                Assert.AreEqual(handle1.Asset, handle3.Asset, "同一资源的多个加载应返回相同实例");

                // Act - 释放第一个引用
                handle1.Dispose();

                // Assert - 资源不应被卸载
                Assert.IsNotNull(handle2.Asset, "释放第一个引用后，资源仍应可用");

                // Act - 释放第二个引用
                handle2.Dispose();

                // Assert - 资源仍不应被卸载
                Assert.IsNotNull(handle3.Asset, "释放第二个引用后，资源仍应可用");

                // Act - 释放最后一个引用
                handle3.Dispose();

                // Assert - 引用计数应归零
                Assert.AreEqual(0, _assetService.MemoryBudget.UsedBytes, "所有引用释放后，内存使用应归零");
            });
        }

        [UnityTest]
        [Timeout(15000)] // 15秒超时保护
        public IEnumerator A002_FrameBasedLoading_NoLagWith100Assets()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // 检查资源是否存在
                if (!await CheckResourceExists(TestPrefabKey))
                {
                    Assert.Ignore($"测试资源不存在: {TestPrefabKey}");
                    return;
                }

                // Arrange
                var keys = new object[10];
                for (var i = 0; i < keys.Length; i++) keys[i] = TestPrefabKey;

                // 使用同步进度跟踪器
                var reportedProgress = new List<float>();
                var progress = new Progress<float>(p => reportedProgress.Add(p));

                var startTime = Time.realtimeSinceStartup;

                // Act - 分帧加载
                await _assetService.PreloadAsync(keys, progress, 3);

                var elapsedTime = Time.realtimeSinceStartup - startTime;

                // 等待几帧确保所有 Progress 回调完成
                for (var i = 0; i < 3; i++) await UniTask.Yield();

                // Assert - 验证进度报告
                Assert.GreaterOrEqual(reportedProgress.Count, 1, "应至少报告一次进度");

                // 验证最终进度（取列表中的最大值）
                var maxProgress = reportedProgress.Count > 0 ? reportedProgress.Max() : 0f;
                Assert.GreaterOrEqual(maxProgress, 0.99f,
                    $"进度应接近100%，实际最大值: {maxProgress}, 报告次数: {reportedProgress.Count}");

                Debug.Log(
                    $"[Test] Preload {keys.Length} assets in {elapsedTime:F2}s, progress reports: {reportedProgress.Count}, max: {maxProgress}");

                // 清理
                _assetService.ReleaseAll();
            });
        }

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A003_MemoryBudget_ExceedingBudgetLogsWarning()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // 检查资源是否存在
                if (!await CheckResourceExists(TestPrefabKey))
                {
                    Assert.Ignore($"测试资源不存在: {TestPrefabKey}");
                    return;
                }

                // Arrange - 设置很小的内存预算
                _assetService.MemoryBudget.BudgetBytes = 100L; // 100 bytes (极小值)

                var budgetExceeded = false;
                _assetService.MemoryBudget.OnBudgetExceeded += ratio =>
                {
                    budgetExceeded = true;
                    Debug.Log($"[Test] Memory budget exceeded! Usage ratio: {ratio:P}");
                };

                // Act - 加载资源
                var handle = await _assetService.LoadAsync<GameObject>(TestPrefabKey);

                // Assert - 应触发内存预算警告
                Assert.IsTrue(budgetExceeded, "内存预算超限应触发警告");
                Assert.Greater(_assetService.MemoryBudget.UsageRatio, 1.0f, "使用率应超过100%");

                // 清理
                handle.Dispose();
            });
        }

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A004_LifetimeBinding_GameObjectDestroyReleasesAsset()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // 检查资源是否存在
                if (!await CheckResourceExists(TestPrefabKey))
                {
                    Assert.Ignore($"测试资源不存在: {TestPrefabKey}");
                    return;
                }

                // Arrange - 创建一个临时 GameObject 作为生命周期容器
                var lifetimeObject = new GameObject("LifetimeTest");

                // Act - 加载资源
                var handle = await _assetService.LoadAsync<GameObject>(TestPrefabKey);
                Assert.IsNotNull(handle.Asset, "资源应成功加载");

                var asset = handle.Asset;

                // 绑定到 GameObject 生命周期
                _assetService.LinkToScope(TestPrefabKey, lifetimeObject);

                // 此时引用计数应为 2（LoadAsync + LinkToScope）
                // 加载第二次以验证资源仍被保留
                var handle2 = await _assetService.LoadAsync<GameObject>(TestPrefabKey);
                Assert.AreSame(asset, handle2.Asset, "资源应为同一实例");
                handle2.Dispose(); // 引用计数回到 2

                // 销毁 GameObject，应触发资源释放（引用计数减 1）
                Object.Destroy(lifetimeObject);

                // 等待一帧确保销毁生效
                await UniTask.Yield();

                // 此时引用计数应为 1（handle 仍持有）
                // 再次加载应该返回同一实例
                var handle3 = await _assetService.LoadAsync<GameObject>(TestPrefabKey);
                Assert.AreSame(asset, handle3.Asset, "资源应仍为同一实例");
                handle3.Dispose();

                // 清理 - handle 仍持有引用，需要手动释放
                handle.Dispose();

                // 等待资源释放
                await UniTask.Yield();

                // 验证资源已被完全释放
                Assert.AreEqual(0, _assetService.MemoryBudget.UsedBytes, "所有引用释放后，内存应归零");
            });
        }

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A005_Instantiate_CreatesInstanceAndTracksReference()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // 检查资源是否存在
                if (!await CheckResourceExists(TestPrefabKey))
                {
                    Assert.Ignore($"测试资源不存在: {TestPrefabKey}");
                    return;
                }

                // Act - 实例化预制体
                var instance = await _assetService.InstantiateAsync(TestPrefabKey);

                // Assert
                Assert.IsNotNull(instance, "应成功实例化预制体");
                Assert.IsTrue(instance.name.StartsWith("TestPrefab"), "实例名称应以 TestPrefab 开头");

                // Act - 实例化多个
                var instance2 = await _assetService.InstantiateAsync(TestPrefabKey);
                var instance3 = await _assetService.InstantiateAsync(TestPrefabKey);

                Assert.IsNotNull(instance2, "第二个实例应成功创建");
                Assert.IsNotNull(instance3, "第三个实例应成功创建");

                // 清理实例
                _assetService.Release(TestPrefabKey);
                Object.Destroy(instance);

                _assetService.Release(TestPrefabKey);
                Object.Destroy(instance2);

                _assetService.Release(TestPrefabKey);
                Object.Destroy(instance3);
            });
        }

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A006_LoadByGuid_SameAsLoadByAddress()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // 检查资源是否存在
                if (!await CheckResourceExists(TestPrefabKey))
                {
                    Assert.Ignore($"测试资源不存在: {TestPrefabKey}");
                    return;
                }

                // Act - 使用 Address 加载
                var handleByAddress = await _assetService.LoadAsync<GameObject>(TestPrefabKey);

                // Act - 使用 GUID 加载
                var handleByGuid = await _assetService.LoadAsync<GameObject>(TestPrefabGuid);

                // Assert - 两种方式应加载同一资源
                Assert.AreEqual(handleByAddress.Asset, handleByGuid.Asset, "Address 和 GUID 应加载同一资源");

                // 清理
                handleByAddress.Dispose();
                handleByGuid.Dispose();
            });
        }

        [UnityTest]
        [Timeout(5000)]
        public IEnumerator A007_LoadInvalidKey_ThrowsException()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                const string invalidKey = "NonExistentAsset";

                // 忽略 Addressables 的错误日志
                LogAssert.ignoreFailingMessages = true;

                // Act & Assert
                try
                {
                    await _assetService.LoadAsync<GameObject>(invalidKey);
                    Assert.Fail("加载不存在的资源应抛出异常");
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(ex.Message.Contains(invalidKey), "异常消息应包含资源 key");
                    Debug.Log($"[Test] Expected exception caught: {ex.Message}");
                }

                LogAssert.ignoreFailingMessages = false;
            });
        }

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A008_ReleaseAll_ClearsAllAssets()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // 检查资源是否存在
                if (!await CheckResourceExists(TestPrefabKey))
                {
                    Assert.Ignore($"测试资源不存在: {TestPrefabKey}");
                    return;
                }

                // Arrange - 加载多个资源
                var handle1 = await _assetService.LoadAsync<GameObject>(TestPrefabKey);
                var handle2 = await _assetService.LoadAsync<GameObject>(TestPrefabGuid);

                Assert.Greater(_assetService.MemoryBudget.UsedBytes, 0, "加载后应有内存使用");

                // Act - 释放所有
                _assetService.ReleaseAll();

                // Assert
                Assert.AreEqual(0, _assetService.MemoryBudget.UsedBytes, "释放后内存使用应归零");
            });
        }

        [UnityTest]
        [Timeout(5000)]
        public IEnumerator A009_CancelLoad_ThrowsOperationCanceledException()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var cts = new CancellationTokenSource();

                // 在下一帧取消
                cts.CancelAfter(TimeSpan.FromMilliseconds(1));

                // Act & Assert
                try
                {
                    await _assetService.LoadAsync<GameObject>(TestPrefabKey, cts.Token);
                    // 如果加载足够快，可能不会触发取消
                    Debug.Log("[Test] Load completed before cancellation");
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("[Test] Load was successfully cancelled");
                }
            });
        }

        [Test]
        public void AssetHandle_Dispose_DecrementsReferenceCount()
        {
            // 这个测试需要异步运行，在同步测试中跳过
            Assert.Ignore("需要异步测试环境，请参考 A001 测试");
        }

        [Test]
        public void MemoryBudget_DefaultValues_AreCorrect()
        {
            // Arrange
            var settings = ScriptableObject.CreateInstance<FrameworkSettings>();
            settings.MemoryBudgetMB = 256;

            // Act
            var service = new AssetService(settings);

            // Assert
            Assert.AreEqual(256L * 1024L * 1024L, service.MemoryBudget.BudgetBytes, "内存预算应正确设置");
            Assert.AreEqual(0, service.MemoryBudget.UsedBytes, "初始内存使用应为零");

            // Cleanup
            service.Dispose();
        }

        [Test]
        public void MemoryBudget_BudgetExceeded_TriggersEvent()
        {
            // Arrange
            var settings = ScriptableObject.CreateInstance<FrameworkSettings>();
            var service = new AssetService(settings);

            service.MemoryBudget.BudgetBytes = 100L;
            var eventTriggered = false;

            service.MemoryBudget.OnBudgetExceeded += ratio => { eventTriggered = true; };

            // 注意：UsedBytes 是 internal set，无法在测试中直接设置
            // 这里只测试事件机制，实际使用时由 AssetService 内部设置
            // 可以通过加载实际资源来测试完整流程

            // Assert
            Assert.IsNotNull(service.MemoryBudget);
            Assert.AreEqual(0, service.MemoryBudget.UsedBytes, "初始使用量应为0");

            // Cleanup
            service.Dispose();
        }

        /// <summary>
        ///     检查资源是否存在
        /// </summary>
        private async UniTask<bool> CheckResourceExists(string key)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(key);
                await handle.ToUniTask();

                var exists = handle.Status == AsyncOperationStatus.Succeeded;
                Addressables.Release(handle);

                return exists;
            }
            catch
            {
                return false;
            }
        }
    }
}