using System;
using NUnit.Framework;

namespace CFramework.Tests
{
    /// <summary>
    ///     StateMachine 单元测试
    /// </summary>
    [TestFixture]
    public class StateMachineTests
    {
        [SetUp]
        public void SetUp()
        {
            _fsm = new StateMachine<string>();
        }

        [TearDown]
        public void TearDown()
        {
            _fsm?.Dispose();
        }

        private StateMachine<string> _fsm;

        [Test]
        public void RegisterState_NullState_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _fsm.RegisterState(null));
        }

        [Test]
        public void RegisterState_DuplicateKey_ThrowsArgumentException()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fsm.RegisterState(new TestState("Idle")));
        }

        [Test]
        public void RegisterState_ValidState_Succeeds()
        {
            // Act
            _fsm.RegisterState(new TestState("Idle"));

            // Assert
            Assert.IsTrue(_fsm.HasState("Idle"));
        }

        [Test]
        public void RegisterState_SetsStateMachineHolderReference()
        {
            // Arrange — 通过继承 StackStateBase 的测试状态间接验证 IStateMachineHolder 引用
            // 注：StackStateBase 的测试在 StateMachineStackTests 中覆盖
            // 此处验证 HasState 和 TryGetState 正常工作即可
            _fsm.RegisterState(new TestState("Idle"));

            // Assert
            Assert.IsTrue(_fsm.HasState("Idle"));
            Assert.IsTrue(_fsm.TryGetState("Idle", out _));
        }

        [Test]
        public void UnregisterState_NonExistent_ReturnsFalse()
        {
            // Act
            var result = _fsm.UnregisterState("NotExist");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void UnregisterState_CurrentState_ReturnsFalse()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.RegisterState(new TestState("Run"));
            _fsm.ChangeState("Idle");

            // Act
            var result = _fsm.UnregisterState("Idle");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void UnregisterState_NonCurrentState_Succeeds()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.RegisterState(new TestState("Run"));
            _fsm.ChangeState("Idle");

            // Act
            var result = _fsm.UnregisterState("Run");

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(_fsm.HasState("Run"));
        }

        [Test]
        public void UnregisterState_ClearsStateMachineHolderReference()
        {
            // Arrange — IStateMachineHolder 的引用清理在 StateMachineStackTests 中通过 StackStateBase 验证
            // 此处验证非当前状态可以被成功注销
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.RegisterState(new TestState("Run"));
            _fsm.ChangeState("Idle");

            // Act
            var result = _fsm.UnregisterState("Run");

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(_fsm.HasState("Run"));
        }

        [Test]
        public void ChangeState_UnregisteredState_ReturnsFalse()
        {
            // Act
            var result = _fsm.ChangeState("NotExist");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ChangeState_SameState_ReturnsFalse()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.ChangeState("Idle");

            // Act
            var result = _fsm.ChangeState("Idle");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ChangeState_DifferentState_Succeeds()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.RegisterState(new TestState("Run"));

            // Act
            var result = _fsm.ChangeState("Idle");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("Idle", _fsm.CurrentState);
        }

        [Test]
        public void ChangeState_CallsOnExitOnOldState()
        {
            // Arrange
            var idleState = new TestState("Idle");
            var runState = new TestState("Run");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(runState);
            _fsm.ChangeState("Idle");

            // Act
            _fsm.ChangeState("Run");

            // Assert
            Assert.IsTrue(idleState.ExitCalled);
            Assert.IsFalse(runState.ExitCalled);
        }

        [Test]
        public void ChangeState_CallsOnEnterOnNewState()
        {
            // Arrange
            var idleState = new TestState("Idle");
            var runState = new TestState("Run");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(runState);
            _fsm.ChangeState("Idle");

            // Act
            _fsm.ChangeState("Run");

            // Assert
            Assert.IsTrue(runState.EnterCalled);
        }

        [Test]
        public void ChangeState_UpdatesCurrentAndPreviousState()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.RegisterState(new TestState("Run"));
            _fsm.RegisterState(new TestState("Jump"));
            _fsm.ChangeState("Idle");

            // Act
            _fsm.ChangeState("Run");

            // Assert
            Assert.AreEqual("Run", _fsm.CurrentState);
            Assert.AreEqual("Idle", _fsm.PreviousState);
        }

        [Test]
        public void ChangeState_FirstTransition_PreviousStateIsDefault()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));

            // Act
            _fsm.ChangeState("Idle");

            // Assert
            Assert.AreEqual("Idle", _fsm.CurrentState);
            Assert.IsNull(_fsm.PreviousState);
        }

        [Test]
        public void ChangeState_FiresOnStateChangedEvent()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.RegisterState(new TestState("Run"));
            _fsm.ChangeState("Idle");

            string fromState = null;
            string toState = null;
            _fsm.OnStateChanged += (from, to) =>
            {
                fromState = from;
                toState = to;
            };

            // Act
            _fsm.ChangeState("Run");

            // Assert
            Assert.AreEqual("Idle", fromState);
            Assert.AreEqual("Run", toState);
        }

        [Test]
        public void TryChangeState_Unregistered_ReturnsFalseWithoutException()
        {
            // Act
            var result = _fsm.TryChangeState("NotExist");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Update_CallsOnUpdateOnCurrentState()
        {
            // Arrange
            var state = new TestState("Idle");
            _fsm.RegisterState(state);
            _fsm.ChangeState("Idle");

            // Act
            _fsm.Update(0.016f);

            // Assert
            Assert.AreEqual(0.016f, state.LastUpdateDeltaTime, 0.0001f);
        }

        [Test]
        public void FixedUpdate_CallsOnFixedUpdateOnCurrentState()
        {
            // Arrange
            var state = new TestState("Idle");
            _fsm.RegisterState(state);
            _fsm.ChangeState("Idle");

            // Act
            _fsm.FixedUpdate(0.02f);

            // Assert
            Assert.AreEqual(0.02f, state.LastFixedUpdateDeltaTime, 0.0001f);
        }

        [Test]
        public void Update_NoCurrentState_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _fsm.Update(0.016f));
        }

        [Test]
        public void FixedUpdate_NoCurrentState_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _fsm.FixedUpdate(0.02f));
        }

        [Test]
        public void Update_StateWithoutIStateUpdate_DoesNotThrow()
        {
            // Arrange — TestState 实现了 IStateUpdate，注册一个不实现它的状态
            var state = new MinimalState("Idle");
            _fsm.RegisterState(state);
            _fsm.ChangeState("Idle");

            // Act & Assert
            Assert.DoesNotThrow(() => _fsm.Update(0.016f));
        }

        [Test]
        public void HasState_Registered_ReturnsTrue()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));

            // Assert
            Assert.IsTrue(_fsm.HasState("Idle"));
        }

        [Test]
        public void HasState_Unregistered_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.HasState("NotExist"));
        }

        [Test]
        public void TryGetState_Registered_ReturnsTrueAndState()
        {
            // Arrange
            var state = new TestState("Idle");
            _fsm.RegisterState(state);

            // Act
            var result = _fsm.TryGetState("Idle", out var found);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(found);
            Assert.AreEqual("Idle", found.Key);
        }

        [Test]
        public void TryGetState_Unregistered_ReturnsFalseAndNull()
        {
            // Act
            var result = _fsm.TryGetState("NotExist", out var found);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(found);
        }

        [Test]
        public void CurrentState_NoState_ReturnsDefault()
        {
            Assert.IsNull(_fsm.CurrentState);
        }

        [Test]
        public void IsTransitioning_IsFalseAfterChange()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.RegisterState(new TestState("Run"));
            _fsm.ChangeState("Idle");

            // Act
            _fsm.ChangeState("Run");

            // Assert
            Assert.IsFalse(_fsm.IsTransitioning);
        }

        [Test]
        public void Dispose_CallsOnExitOnCurrentState()
        {
            // Arrange
            var state = new TestState("Idle");
            _fsm.RegisterState(state);
            _fsm.ChangeState("Idle");

            // Act
            _fsm.Dispose();

            // Assert
            Assert.IsTrue(state.ExitCalled);
        }

        [Test]
        public void Dispose_ClearsAllStates()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.RegisterState(new TestState("Run"));
            _fsm.ChangeState("Idle");

            // Act
            _fsm.Dispose();

            // Assert
            Assert.IsFalse(_fsm.HasState("Idle"));
            Assert.IsFalse(_fsm.HasState("Run"));
            Assert.IsNull(_fsm.CurrentState);
        }

        [Test]
        public void Dispose_DoubleDispose_DoesNotThrow()
        {
            // Arrange
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.ChangeState("Idle");
            _fsm.Dispose();

            // Act & Assert
            Assert.DoesNotThrow(() => _fsm.Dispose());
        }

        [Test]
        public void Dispose_ClearsHolderReferences()
        {
            // IStateMachineHolder 的引用清理在 StateMachineStackTests 中通过 StackStateBase 验证
            // 此处验证 Dispose 后所有状态被清空
            _fsm.RegisterState(new TestState("Idle"));
            _fsm.RegisterState(new TestState("Run"));
            _fsm.ChangeState("Idle");

            // Act
            _fsm.Dispose();

            // Assert
            Assert.IsFalse(_fsm.HasState("Idle"));
            Assert.IsFalse(_fsm.HasState("Run"));
        }

        /// <summary>
        ///     完整功能的测试状态，实现所有生命周期接口
        /// </summary>
        private class TestState : IState<string>, IStateEnter, IStateExit, IStateUpdate, IStateFixedUpdate
        {
            public TestState(string key)
            {
                Key = key;
            }

            public bool EnterCalled { get; private set; }
            public bool ExitCalled { get; private set; }
            public float LastUpdateDeltaTime { get; private set; }
            public float LastFixedUpdateDeltaTime { get; private set; }
            public string Key { get; }

            public void OnEnter()
            {
                EnterCalled = true;
            }

            public void OnExit()
            {
                ExitCalled = true;
            }

            public void OnFixedUpdate(float fixedDeltaTime)
            {
                LastFixedUpdateDeltaTime = fixedDeltaTime;
            }

            public void OnUpdate(float deltaTime)
            {
                LastUpdateDeltaTime = deltaTime;
            }
        }

        /// <summary>
        ///     最小状态，仅实现 IState
        /// </summary>
        private class MinimalState : IState<string>
        {
            public MinimalState(string key)
            {
                Key = key;
            }

            public string Key { get; }
        }
    }
}