using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using UnityEngine.TestTools;

namespace CFramework.Tests
{
    /// <summary>
    ///     事件总线单元测试
    /// </summary>
    [TestFixture]
    public class EventBusTests
    {
        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _eventBus?.Dispose();
        }

        private EventBus _eventBus;

        [Test]
        public void E001_ExceptionIsolation_SingleHandlerExceptionDoesNotAffectOthers()
        {
            // Arrange
            var callOrder = new List<int>();
            var exceptionThrown = false;

            // 注册三个处理器，第二个会抛出异常
            var disposable1 = _eventBus.Subscribe<TestEvent>(e => callOrder.Add(1), 10);
            var disposable2 = _eventBus.Subscribe<TestEvent>(e =>
            {
                callOrder.Add(2);
                throw new InvalidOperationException("Test exception");
            }, 5);
            var disposable3 = _eventBus.Subscribe<TestEvent>(e => callOrder.Add(3), 0);

            // 注册异常处理器
            _eventBus.OnHandlerError += (ex, evt, handler) => exceptionThrown = true;

            // Act
            _eventBus.Publish(new TestEvent { Value = 1 });

            // Assert
            Assert.AreEqual(new[] { 1, 2, 3 }, callOrder.ToArray(), "所有处理器都应被执行");
            Assert.IsTrue(exceptionThrown, "异常应该被捕获");

            // Cleanup
            disposable1.Dispose();
            disposable2.Dispose();
            disposable3.Dispose();
        }

        [UnityTest]
        [Timeout(5000)] // 5秒超时保护
        public IEnumerator E002_AsyncEventTimeout_HandlerTimeout_Success()
        {
            // Arrange
            var timeoutOccurred = false;
            var handlerCalled = false;

            // 注册一个会超时的异步处理器（缩短超时时间）
            var disposable = _eventBus.SubscribeAsync<TestAsyncEvent>(async (e, ct) =>
            {
                handlerCalled = true;
                // 模拟耗时操作，但使用较短的延迟
                await UniTask.Delay(TimeSpan.FromSeconds(0.5), cancellationToken: ct);
            });

            _eventBus.OnHandlerError += (ex, evt, handler) =>
            {
                if (ex is TimeoutException) timeoutOccurred = true;
            };

            // 使用超时 CancellationToken
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            try
            {
                // Act
                yield return UniTask.ToCoroutine(async () => { await _eventBus.PublishAsync(new TestAsyncEvent()); });

                // Assert
                Assert.IsTrue(handlerCalled, "处理器应该被调用");
                Assert.IsTrue(timeoutOccurred, "应该触发超时错误回调");
            }
            finally
            {
                cts?.Dispose();
                disposable.Dispose();
            }
        }

        [Test]
        public void E003_PriorityExecution_HigherPriorityExecutesFirst()
        {
            // Arrange
            var executionOrder = new List<int>();

            // 注册多个不同优先级的处理器
            var disposable1 = _eventBus.Subscribe<TestEvent>(e => executionOrder.Add(1), 1);
            var disposable2 = _eventBus.Subscribe<TestEvent>(e => executionOrder.Add(2), 10);
            var disposable3 = _eventBus.Subscribe<TestEvent>(e => executionOrder.Add(3), 5);
            var disposable4 = _eventBus.Subscribe<TestEvent>(e => executionOrder.Add(4), 10);

            // Act
            _eventBus.Publish(new TestEvent { Value = 1 });

            // Assert
            Assert.AreEqual(4, executionOrder.Count, "所有处理器都应被执行");
            // 优先级10的处理器应该先执行（顺序可能是2或4）
            Assert.IsTrue(executionOrder[0] == 2 || executionOrder[0] == 4, "优先级10的处理器应先执行");
            Assert.IsTrue(executionOrder[1] == 2 || executionOrder[1] == 4, "优先级10的处理器应先执行");
            // 然后是优先级5的处理器
            Assert.AreEqual(3, executionOrder[2], "优先级5的处理器应第二个执行");
            // 最后是优先级1的处理器
            Assert.AreEqual(1, executionOrder[3], "优先级1的处理器应最后执行");

            // Cleanup
            disposable1.Dispose();
            disposable2.Dispose();
            disposable3.Dispose();
            disposable4.Dispose();
        }

        [Test]
        public void E004_StructEventNoBoxing_PerformanceBenchmark()
        {
            // Arrange
            const int iterations = 100000;
            var counter = 0;

            var disposable = _eventBus.Subscribe<TestStructEvent>(e => counter++);

            // Act
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++) _eventBus.Publish(new TestStructEvent { Value = i });

            stopwatch.Stop();

            // Assert
            Assert.AreEqual(iterations, counter, "所有事件都应被处理");
            Assert.Less(stopwatch.ElapsedMilliseconds, 500,
                $"发布 {iterations} 个 struct 事件应在 500ms 内完成，实际耗时: {stopwatch.ElapsedMilliseconds}ms");

            // Cleanup
            disposable.Dispose();
        }

        [Test]
        public void SubscribeAndPublish_EventReceived_Success()
        {
            // Arrange
            var received = false;
            var receivedValue = 0;

            // Act
            var disposable = _eventBus.Subscribe<TestEvent>(e =>
            {
                received = true;
                receivedValue = e.Value;
            });

            _eventBus.Publish(new TestEvent { Value = 42 });

            // Assert
            Assert.IsTrue(received, "事件应该被接收");
            Assert.AreEqual(42, receivedValue, "事件数据应该正确传递");

            // Cleanup
            disposable.Dispose();
        }

        [Test]
        public void Unsubscribe_NoLongerReceivesEvents()
        {
            // Arrange
            var counter = 0;

            // Act
            var disposable = _eventBus.Subscribe<TestEvent>(e => counter++);
            _eventBus.Publish(new TestEvent { Value = 1 });

            // 取消订阅
            disposable.Dispose();
            _eventBus.Publish(new TestEvent { Value = 2 });

            // Assert
            Assert.AreEqual(1, counter, "取消订阅后不应接收事件");
        }

        [Test]
        public void Receive_ReactiveSubscription_Success()
        {
            // Arrange
            var receivedValue = 0;

            // Act
            var disposable = _eventBus.Receive<TestEvent>()
                .Subscribe(e => receivedValue = e.Value);

            _eventBus.Publish(new TestEvent { Value = 123 });

            // Assert
            Assert.AreEqual(123, receivedValue, "响应式订阅应正确接收事件");

            // Cleanup
            disposable.Dispose();
        }

        private struct TestEvent : IEvent
        {
            public int Value;
        }

        private struct TestStructEvent : IEvent
        {
            public int Value;
        }

        private class TestAsyncEvent : IAsyncEvent
        {
            public TimeSpan Timeout => TimeSpan.FromSeconds(1); // 缩短超时时间
        }
    }
}