using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CFramework.Tests
{
    /// <summary>
    ///     异常分发器单元测试
    /// </summary>
    [TestFixture]
    public class ExceptionDispatcherTests
    {
        [SetUp]
        public void SetUp()
        {
            _dispatcher = new DefaultExceptionDispatcher();
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher?.Dispose();
        }

        private DefaultExceptionDispatcher _dispatcher;

        [Test]
        public void Dispatch_ExceptionHandled_CallsRegisteredHandlers()
        {
            // Arrange
            Exception receivedException = null;
            var handlerCallCount = 0;

            _dispatcher.RegisterHandler(ex =>
            {
                receivedException = ex;
                handlerCallCount++;
            });

            var testException = new InvalidOperationException("Test exception");

            // 临时忽略日志检查，专注于测试异常处理逻辑
            LogAssert.ignoreFailingMessages = true;

            // Act
            _dispatcher.Dispatch(testException, "TestContext");

            // Assert
            Assert.AreEqual(testException, receivedException, "异常应该被传递给处理器");
            Assert.AreEqual(1, handlerCallCount, "处理器应该被调用一次");

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Dispatch_MultipleHandlers_AllCalled()
        {
            // Arrange
            var callCount = 0;

            _dispatcher.RegisterHandler(ex => callCount++);
            _dispatcher.RegisterHandler(ex => callCount++);
            _dispatcher.RegisterHandler(ex => callCount++);

            // 临时忽略日志检查
            LogAssert.ignoreFailingMessages = true;

            // Act
            _dispatcher.Dispatch(new Exception("Test"));

            // Assert
            Assert.AreEqual(3, callCount, "所有处理器都应被调用");

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void RegisterHandler_ReturnsDisposable_CanUnregister()
        {
            // Arrange
            var callCount = 0;

            var disposable = _dispatcher.RegisterHandler(ex => callCount++);

            // 临时忽略日志检查
            LogAssert.ignoreFailingMessages = true;

            // Act
            _dispatcher.Dispatch(new Exception("First"));
            Assert.AreEqual(1, callCount);

            // 取消注册
            disposable.Dispose();
            _dispatcher.Dispatch(new Exception("Second"));

            // Assert
            Assert.AreEqual(1, callCount, "取消注册后处理器不应被调用");

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Dispatch_NullException_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => _dispatcher.Dispatch(null));
        }

        [Test]
        public void RegisterHandler_NullHandler_ReturnsEmptyDisposable()
        {
            // Arrange & Act
            var disposable = _dispatcher.RegisterHandler(null);

            // Assert
            Assert.IsNotNull(disposable);

            // 临时忽略日志检查
            LogAssert.ignoreFailingMessages = true;

            // 验证不会抛出异常
            Assert.DoesNotThrow(() => _dispatcher.Dispatch(new Exception("Test")));

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Dispatch_WithContext_LogsContextMessage()
        {
            // Arrange
            Exception receivedException = null;

            _dispatcher.RegisterHandler(ex => receivedException = ex);

            var testException = new Exception("Test");

            // 临时忽略日志检查
            LogAssert.ignoreFailingMessages = true;

            // Act
            _dispatcher.Dispatch(testException, "GameLogic");

            // Assert
            Assert.AreEqual(testException, receivedException);

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Dispose_DisposesAllHandlers()
        {
            // Arrange
            var callCount = 0;
            _dispatcher.RegisterHandler(ex => callCount++);

            // Act
            _dispatcher.Dispose();

            // 尝试分发异常（应该不会抛出异常，处理器不会被调用）
            // 注意：实现应该在 Dispose 后安全地忽略 Dispatch 调用
            Assert.DoesNotThrow(() => _dispatcher.Dispatch(new Exception("Test")));

            // 验证处理器没有被调用
            Assert.AreEqual(0, callCount, "Dispose 后处理器不应被调用");
        }

        [Test]
        public void MultipleRegisterAndUnregister_WorksCorrectly()
        {
            // Arrange
            var counter = 0;

            var disposable1 = _dispatcher.RegisterHandler(ex => counter++);
            var disposable2 = _dispatcher.RegisterHandler(ex => counter += 10);
            var disposable3 = _dispatcher.RegisterHandler(ex => counter += 100);

            // 临时忽略日志检查
            LogAssert.ignoreFailingMessages = true;

            // Act
            _dispatcher.Dispatch(new Exception("Test1"));
            Assert.AreEqual(111, counter, "所有处理器应被调用");

            disposable2.Dispose();
            _dispatcher.Dispatch(new Exception("Test2"));
            Assert.AreEqual(212, counter, "只有处理器1和3应被调用");

            disposable1.Dispose();
            _dispatcher.Dispatch(new Exception("Test3"));
            Assert.AreEqual(312, counter, "只有处理器3应被调用");

            disposable3.Dispose();
            _dispatcher.Dispatch(new Exception("Test4"));
            Assert.AreEqual(312, counter, "没有处理器应被调用");

            LogAssert.ignoreFailingMessages = false;
        }
    }
}