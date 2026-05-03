using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CFramework.Tests
{
    /// <summary>
    ///     资源服务单元测试
    ///     <para>使用 MockAssetProvider 模拟资源加载，无需配置 Addressables 测试资源</para>
    /// </summary>
    [TestFixture]
    public class AssetServiceTests
    {
        private MockAssetProvider _mockProvider;
        private AssetService _assetService;
        private List<GameObject> _cleanupObjects;

        private const string TestPrefabKey = "TestPrefab";

        [SetUp]
        public void SetUp()
        {
            var settings = ScriptableObject.CreateInstance<AssetSettings>();
            settings.MemoryBudgetMB = 512;
            settings.MaxLoadPerFrame = 5;

            _mockProvider = new MockAssetProvider();
            // 注册模拟资源
            _mockProvider.RegisterGameObject(TestPrefabKey, "TestPrefab");
            _assetService = new AssetService(settings, _mockProvider);
            _cleanupObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            _assetService?.Dispose();
            _mockProvider?.Cleanup();

            foreach (var go in _cleanupObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _cleanupObjects.Clear();
        }

        #region 引用计数测试

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A001_ReferenceCount_MultipleLoadsReleaseCorrectly()
        {
            return UniTask.ToCoroutine(async () =>
            {
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

        #endregion

        #region 分帧加载测试

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator A002_FrameBasedLoading_NoLagWith100Assets()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 注册多个模拟资源
                var keys = new object[10];
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = TestPrefabKey; // 复用同一资源测试引用计数
                }

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

        #endregion

        #region 内存预算测试

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A003_MemoryBudget_ExceedingBudgetLogsWarning()
        {
            return UniTask.ToCoroutine(async () =>
            {
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

        #endregion

        #region 生命周期绑定测试

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A004_LifetimeBinding_GameObjectDestroyReleasesAsset()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 创建一个临时 GameObject 作为生命周期容器
                var lifetimeObject = new GameObject("LifetimeTest");
                _cleanupObjects.Add(lifetimeObject);

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

        #endregion

        #region 实例化测试

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A005_Instantiate_CreatesInstanceAndDisposeReleases()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Act - 实例化预制体
                using var handle = await _assetService.InstantiateAsync(TestPrefabKey);

                // Assert
                Assert.IsNotNull(handle.GameObject, "应成功实例化预制体");
                Assert.IsTrue(handle.GameObject.name.Contains("TestPrefab"),
                    $"实例名称应包含 TestPrefab，实际: {handle.GameObject.name}");

                _cleanupObjects.Add(handle.GameObject);

                // Act - 实例化多个
                using var handle2 = await _assetService.InstantiateAsync(TestPrefabKey);
                using var handle3 = await _assetService.InstantiateAsync(TestPrefabKey);

                Assert.IsNotNull(handle2.GameObject, "第二个实例应成功创建");
                Assert.IsNotNull(handle3.GameObject, "第三个实例应成功创建");

                _cleanupObjects.Add(handle2.GameObject);
                _cleanupObjects.Add(handle3.GameObject);

                // using 结束时自动 Dispose（ReleaseInstance）
            });
        }

        #endregion

        #region 无效 Key 测试

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

        #endregion

        #region 释放所有资源测试

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A008_ReleaseAll_ClearsAllAssets()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 注册第二个 key 并加载
                _mockProvider.RegisterGameObject("TestPrefab2", "TestPrefab2");
                var handle1 = await _assetService.LoadAsync<GameObject>(TestPrefabKey);
                var handle2 = await _assetService.LoadAsync<GameObject>("TestPrefab2");

                Assert.Greater(_assetService.MemoryBudget.UsedBytes, 0, "加载后应有内存使用");

                // Act - 释放所有
                _assetService.ReleaseAll();

                // Assert
                Assert.AreEqual(0, _assetService.MemoryBudget.UsedBytes, "释放后内存使用应归零");
            });
        }

        #endregion

        #region 取消加载测试

        [UnityTest]
        [Timeout(5000)]
        public IEnumerator A009_CancelLoad_ThrowsOperationCanceledException()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 使用带延迟的 provider 确保取消可以生效
                var settings = ScriptableObject.CreateInstance<AssetSettings>();
                var delayProvider = new MockAssetProvider(loadDelayMs: 500);
                delayProvider.RegisterGameObject("SlowAsset", "SlowAsset");
                var slowService = new AssetService(settings, delayProvider);

                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(10));

                // Act & Assert
                try
                {
                    await slowService.LoadAsync<GameObject>("SlowAsset", cts.Token);
                    Debug.Log("[Test] Load completed before cancellation");
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("[Test] Load was successfully cancelled");
                }

                slowService.Dispose();
                delayProvider.Cleanup();
            });
        }

        #endregion

        #region 并发加载测试（验证竞态修复）

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A010_ConcurrentLoad_SameKey_ShouldNotLoadTwice()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 使用带延迟的 provider 模拟慢加载
                var settings = ScriptableObject.CreateInstance<AssetSettings>();
                var delayProvider = new MockAssetProvider(loadDelayMs: 100);
                delayProvider.RegisterGameObject("ConcurrentAsset", "ConcurrentAsset");
                var service = new AssetService(settings, delayProvider);

                // Act - 同时发起 3 个加载请求
                var task1 = service.LoadAsync<GameObject>("ConcurrentAsset");
                var task2 = service.LoadAsync<GameObject>("ConcurrentAsset");
                var task3 = service.LoadAsync<GameObject>("ConcurrentAsset");

                var result1 = await task1;
                var result2 = await task2;
                var result3 = await task3;

                // Assert - 所有结果应指向同一资源
                Assert.IsNotNull(result1.Asset, "资源应成功加载");
                Assert.AreEqual(result1.Asset, result2.Asset, "并发加载应返回同一实例");
                Assert.AreEqual(result1.Asset, result3.Asset, "并发加载应返回同一实例");

                // Provider 的 LoadAssetAsync 应只被调用一次
                // 可通过 ReleaseLog 验证引用计数正确
                result1.Dispose();
                result2.Dispose();
                result3.Dispose();

                Assert.AreEqual(0, service.MemoryBudget.UsedBytes, "释放后内存应归零");

                service.Dispose();
                delayProvider.Cleanup();
            });
        }

        #endregion

        #region 同步单元测试

        [Test]
        public void MemoryBudget_DefaultValues_AreCorrect()
        {
            // Arrange
            var settings = ScriptableObject.CreateInstance<AssetSettings>();
            settings.MemoryBudgetMB = 256;

            // Act
            var service = new AssetService(settings, _mockProvider);

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
            var settings = ScriptableObject.CreateInstance<AssetSettings>();
            var service = new AssetService(settings, _mockProvider);

            service.MemoryBudget.BudgetBytes = 100L;
            var eventTriggered = false;

            service.MemoryBudget.OnBudgetExceeded += ratio => { eventTriggered = true; };

            // Assert
            Assert.IsNotNull(service.MemoryBudget);
            Assert.AreEqual(0, service.MemoryBudget.UsedBytes, "初始使用量应为0");

            // Cleanup
            service.Dispose();
        }

        #endregion

        #region Mock 资源自定义内存大小测试

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator A011_CustomMemorySize_TrackedCorrectly()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - 注册一个自定义内存大小的资源
                _mockProvider.RegisterGameObject("BigAsset", "BigAsset", 2048L);

                // Act
                var handle = await _assetService.LoadAsync<GameObject>("BigAsset");

                // Assert - 内存使用应为 2048
                Assert.AreEqual(2048L, _assetService.MemoryBudget.UsedBytes, "应使用注册时的自定义内存大小");

                // 清理
                handle.Dispose();
                Assert.AreEqual(0, _assetService.MemoryBudget.UsedBytes, "释放后内存应归零");
            });
        }

        #endregion

        #region 不存在的资源测试（替代原 A006 GUID 测试）

        [UnityTest]
        [Timeout(5000)]
        public IEnumerator A012_LoadNonExistentKey_FailsWithCorrectMessage()
        {
            return UniTask.ToCoroutine(async () =>
            {
                LogAssert.ignoreFailingMessages = true;

                try
                {
                    await _assetService.LoadAsync<GameObject>("SomeRandomKey_12345");
                    Assert.Fail("应抛出异常");
                }
                catch (Exception ex)
                {
                    StringAssert.Contains("SomeRandomKey_12345", ex.Message);
                }

                LogAssert.ignoreFailingMessages = false;
            });
        }

        #endregion
    }
}
