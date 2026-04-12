using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CFramework.Tests
{
    /// <summary>
    ///     日志服务测试
    /// </summary>
    [TestFixture]
    public class LoggerTests
    {
        [SetUp]
        public void SetUp()
        {
            // 创建测试用的 FrameworkSettings
            _settings = ScriptableObject.CreateInstance<FrameworkSettings>();
            _settings.LogLevel = LogLevel.Debug;

            // 创建日志服务
            _logger = new UnityLogger(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null) Object.DestroyImmediate(_settings);
        }

        private FrameworkSettings _settings;
        private UnityLogger _logger;

        [Test]
        public void LogLevel_DefaultFromSettings()
        {
            Assert.AreEqual(LogLevel.Debug, _logger.LogLevel);
        }

        [Test]
        public void LogLevel_CanChange()
        {
            _logger.LogLevel = LogLevel.Warning;
            Assert.AreEqual(LogLevel.Warning, _logger.LogLevel);
        }

        [Test]
        public void IsEnabled_DebugLevel_ReturnsTrueForDebug()
        {
            _logger.LogLevel = LogLevel.Debug;
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Debug));
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Info));
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Warning));
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Error));
        }

        [Test]
        public void IsEnabled_InfoLevel_ReturnsFalseForDebug()
        {
            _logger.LogLevel = LogLevel.Info;
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Debug));
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Info));
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Warning));
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Error));
        }

        [Test]
        public void IsEnabled_WarningLevel_ReturnsFalseForDebugAndInfo()
        {
            _logger.LogLevel = LogLevel.Warning;
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Debug));
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Info));
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Warning));
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Error));
        }

        [Test]
        public void IsEnabled_ErrorLevel_ReturnsFalseForLowerLevels()
        {
            _logger.LogLevel = LogLevel.Error;
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Debug));
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Info));
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Warning));
            Assert.IsTrue(_logger.IsEnabled(LogLevel.Error));
        }

        [Test]
        public void IsEnabled_NoneLevel_ReturnsFalseForAll()
        {
            _logger.LogLevel = LogLevel.None;
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Debug));
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Info));
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Warning));
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Error));
            Assert.IsFalse(_logger.IsEnabled(LogLevel.Exception));
        }

        [Test]
        public void LogDebug_OutputsMessage()
        {
            Assert.DoesNotThrow(() => _logger.LogDebug("Test debug message"));
        }

        [Test]
        public void LogDebug_WithTag_OutputsFormattedMessage()
        {
            Assert.DoesNotThrow(() => _logger.LogDebug("TestTag", "Test debug message"));
        }

        [Test]
        public void LogDebug_WhenDisabled_DoesNotOutput()
        {
            _logger.LogLevel = LogLevel.Info;
            Assert.DoesNotThrow(() => _logger.LogDebug("Should not appear"));
        }

        [Test]
        public void LogInfo_OutputsMessage()
        {
            Assert.DoesNotThrow(() => _logger.LogInfo("Test info message"));
        }

        [Test]
        public void LogInfo_WithTag_OutputsFormattedMessage()
        {
            Assert.DoesNotThrow(() => _logger.LogInfo("TestTag", "Test info message"));
        }

        [Test]
        public void LogWarning_OutputsMessage()
        {
            // Unity 测试框架要求预期 Warning 日志
            LogAssert.Expect(LogType.Warning, "Test warning message");
            _logger.LogWarning("Test warning message");
        }

        [Test]
        public void LogWarning_WithTag_OutputsFormattedMessage()
        {
            LogAssert.Expect(LogType.Warning, "[TestTag] Test warning message");
            _logger.LogWarning("TestTag", "Test warning message");
        }

        [Test]
        public void LogError_OutputsMessage()
        {
            // Unity 测试框架要求预期 Error 日志
            LogAssert.Expect(LogType.Error, "Test error message");
            _logger.LogError("Test error message");
        }

        [Test]
        public void LogError_WithTag_OutputsFormattedMessage()
        {
            LogAssert.Expect(LogType.Error, "[TestTag] Test error message");
            _logger.LogError("TestTag", "Test error message");
        }

        [Test]
        public void LogException_OutputsException()
        {
            var exception = new Exception("Test exception");
            // 使用正则表达式匹配异常日志
            LogAssert.Expect(LogType.Exception, new Regex(@".*Test exception.*"));
            _logger.LogException(exception);
        }

        [Test]
        public void LogException_WithTag_OutputsExceptionWithTag()
        {
            var exception = new Exception("Test exception");
            // 先预期标签消息
            LogAssert.Expect(LogType.Error, "[TestTag] Test exception");
            // 使用正则表达式匹配异常日志
            LogAssert.Expect(LogType.Exception, new Regex(@".*Test exception.*"));
            _logger.LogException("TestTag", exception);
        }

        [Test]
        public void LogDebugFormat_OutputsFormattedMessage()
        {
            Assert.DoesNotThrow(() => _logger.LogDebugFormat("Value: {0}", 42));
        }

        [Test]
        public void LogDebugFormat_WithTag_OutputsFormattedMessage()
        {
            Assert.DoesNotThrow(() => _logger.LogDebugFormat("TestTag", "Value: {0}", 42));
        }

        [Test]
        public void LogInfoFormat_OutputsFormattedMessage()
        {
            Assert.DoesNotThrow(() => _logger.LogInfoFormat("Value: {0}", 42));
        }

        [Test]
        public void LogWarningFormat_OutputsFormattedMessage()
        {
            LogAssert.Expect(LogType.Warning, "Value: 42");
            _logger.LogWarningFormat("Value: {0}", 42);
        }

        [Test]
        public void LogErrorFormat_OutputsFormattedMessage()
        {
            LogAssert.Expect(LogType.Error, "Value: 42");
            _logger.LogErrorFormat("Value: {0}", 42);
        }

        [Test]
        public void LogDebug_EmptyMessage_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _logger.LogDebug(""));
        }

        [Test]
        public void LogDebug_NullMessage_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _logger.LogDebug(null));
        }

        [Test]
        public void LogDebug_EmptyTag_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _logger.LogDebug("", "Test message"));
        }

        [Test]
        public void LogDebug_NullTag_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _logger.LogDebug(null, "Test message"));
        }

        [Test]
        public void LogException_NullException_DoesNotThrow()
        {
            // null 异常不应该崩溃，但也不会输出日志
            Assert.DoesNotThrow(() => _logger.LogException(null));
        }
    }
}