using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CFramework.Tests
{
    /// <summary>
    ///     StateMachineStack 单元测试
    /// </summary>
    [TestFixture]
    public class StateMachineStackTests
    {
        [SetUp]
        public void SetUp()
        {
            _fsm = new StateMachineStack<string>();
        }

        [TearDown]
        public void TearDown()
        {
            _fsm?.Dispose();
        }

        private StateMachineStack<string> _fsm;

        [Test]
        public void RegisterState_NullState_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _fsm.RegisterState(null));
        }

        [Test]
        public void RegisterState_NonStackState_ThrowsArgumentException()
        {
            // Arrange — 传入仅实现 IState 的对象
            var nonStackState = new NonStackState("Idle");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fsm.RegisterState(nonStackState));
        }

        [Test]
        public void RegisterState_DuplicateKey_ThrowsArgumentException()
        {
            // Arrange
            _fsm.RegisterState(new StackTestState("Idle"));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _fsm.RegisterState(new StackTestState("Idle")));
        }

        [Test]
        public void RegisterState_ValidStackState_Succeeds()
        {
            // Act
            _fsm.RegisterState(new StackTestState("Idle"));

            // Assert
            Assert.IsTrue(_fsm.HasState("Idle"));
        }

        [Test]
        public void RegisterState_SetsStackStateBaseReference()
        {
            // Arrange
            var state = new StackTestState("Idle");

            // Act
            _fsm.RegisterState(state);

            // Assert
            Assert.IsNotNull(state.GetStateMachine());
        }

        [Test]
        public void UnregisterState_NonExistent_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.UnregisterState("NotExist"));
        }

        [Test]
        public void UnregisterState_StateInStack_ReturnsFalse()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            var result = _fsm.UnregisterState("Idle");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void UnregisterState_StateNotInStack_Succeeds()
        {
            // Arrange
            RegisterStates("Idle", "Menu", "Settings");
            _fsm.ChangeState("Idle");

            // Act
            var result = _fsm.UnregisterState("Menu");

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(_fsm.HasState("Menu"));
        }

        [Test]
        public void UnregisterState_ClearsStackStateBaseReference()
        {
            // Arrange
            var state = new StackTestState("Idle");
            _fsm.RegisterState(state);
            _fsm.ChangeState("Idle");
            _fsm.ChangeState("Other"); // 需要另一个状态来让 Idle 不在栈中
            // 注意 ChangeState 是替换栈顶，所以 Idle 不在栈中了
            // 但 Idle 可能还在栈中，取决于 ChangeState 的行为
            // 先简单测试：注册多个，切换后注销非栈内状态
            _fsm.Dispose();
            _fsm = new StateMachineStack<string>();

            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.ChangeState("Idle"); // 栈: [Idle]

            // Act
            var result = _fsm.UnregisterState("Menu");

            // Assert
            Assert.IsTrue(result);
            Assert.IsNull(menuState.GetStateMachine());
        }

        [Test]
        public void ChangeState_Unregistered_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.ChangeState("NotExist"));
        }

        [Test]
        public void ChangeState_FromEmpty_Succeeds()
        {
            // Arrange
            _fsm.RegisterState(new StackTestState("Idle"));

            // Act
            var result = _fsm.ChangeState("Idle");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("Idle", _fsm.CurrentState);
            Assert.AreEqual(1, _fsm.StackDepth);
        }

        [Test]
        public void ChangeState_ReplacesStackTop()
        {
            // Arrange
            RegisterStates("Idle", "Menu", "Settings");
            _fsm.ChangeState("Idle");

            // Act
            _fsm.ChangeState("Menu");

            // Assert
            Assert.AreEqual("Menu", _fsm.CurrentState);
            Assert.AreEqual(1, _fsm.StackDepth);
        }

        [Test]
        public void ChangeState_CallsOnExitOnOldTopAndOnEnterOnNew()
        {
            // Arrange
            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.ChangeState("Idle");

            // Act
            _fsm.ChangeState("Menu");

            // Assert
            Assert.IsTrue(idleState.ExitCalled);
            Assert.IsTrue(menuState.EnterCalled);
        }

        [Test]
        public void ChangeState_FiresOnStateChanged()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");

            string from = null, to = null;
            _fsm.OnStateChanged += (f, t) =>
            {
                from = f;
                to = t;
            };

            // Act
            _fsm.ChangeState("Menu");

            // Assert
            Assert.AreEqual("Idle", from);
            Assert.AreEqual("Menu", to);
        }

        [Test]
        public void ChangeState_FiresOnStackChanged()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");

            IReadOnlyList<string> snapshot = null;
            _fsm.OnStackChanged += s => snapshot = s;

            // Act
            _fsm.ChangeState("Menu");

            // Assert
            Assert.IsNotNull(snapshot);
            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual("Menu", snapshot[0]);
        }

        [Test]
        public void Push_Unregistered_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.Push("NotExist"));
        }

        [Test]
        public void Push_SameAsCurrentTop_ReturnsFalse()
        {
            // Arrange
            RegisterStates("Idle");
            _fsm.ChangeState("Idle");

            // Act
            var result = _fsm.Push("Idle");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Push_NewState_IncreasesStackDepth()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");

            // Act
            var result = _fsm.Push("Menu");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, _fsm.StackDepth);
            Assert.AreEqual("Menu", _fsm.CurrentState);
        }

        [Test]
        public void Push_CallsOnPauseOnOldTop()
        {
            // Arrange
            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.ChangeState("Idle");

            // Act
            _fsm.Push("Menu");

            // Assert
            Assert.IsTrue(idleState.PauseCalled);
        }

        [Test]
        public void Push_CallsOnEnterOnNewState()
        {
            // Arrange
            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.ChangeState("Idle");

            // Act
            _fsm.Push("Menu");

            // Assert
            Assert.IsTrue(menuState.EnterCalled);
        }

        [Test]
        public void Push_MultipleStates_CorrectStackOrder()
        {
            // Arrange
            RegisterStates("Idle", "Menu", "Settings", "Help");
            _fsm.ChangeState("Idle");

            // Act
            _fsm.Push("Menu");
            _fsm.Push("Settings");
            _fsm.Push("Help");

            // Assert
            Assert.AreEqual(4, _fsm.StackDepth);
            Assert.AreEqual("Help", _fsm.CurrentState);

            var snapshot = _fsm.GetStackSnapshot();
            Assert.AreEqual("Idle", snapshot[0]);
            Assert.AreEqual("Menu", snapshot[1]);
            Assert.AreEqual("Settings", snapshot[2]);
            Assert.AreEqual("Help", snapshot[3]);
        }

        [Test]
        public void Push_FiresOnStackChanged()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");

            IReadOnlyList<string> snapshot = null;
            _fsm.OnStackChanged += s => snapshot = s;

            // Act
            _fsm.Push("Menu");

            // Assert
            Assert.IsNotNull(snapshot);
            Assert.AreEqual(2, snapshot.Count);
        }

        [Test]
        public void Pop_EmptyStack_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.Pop());
        }

        [Test]
        public void Pop_SingleState_ReturnsFalse()
        {
            // Arrange
            RegisterStates("Idle");
            _fsm.ChangeState("Idle");

            // Act
            var result = _fsm.Pop();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Pop_MultipleStates_DecreasesStackDepth()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            var result = _fsm.Pop();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _fsm.StackDepth);
            Assert.AreEqual("Idle", _fsm.CurrentState);
        }

        [Test]
        public void Pop_CallsOnExitOnTopAndOnResumeOnNewTop()
        {
            // Arrange
            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            _fsm.Pop();

            // Assert
            Assert.IsTrue(menuState.ExitCalled);
            Assert.IsTrue(idleState.ResumeCalled);
        }

        [Test]
        public void Pop_UpdatesPreviousState()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            _fsm.Pop();

            // Assert
            Assert.AreEqual("Menu", _fsm.PreviousState);
        }

        [Test]
        public void Pop_FiresOnStateChanged()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            string from = null, to = null;
            _fsm.OnStateChanged += (f, t) =>
            {
                from = f;
                to = t;
            };

            // Act
            _fsm.Pop();

            // Assert
            Assert.AreEqual("Menu", from);
            Assert.AreEqual("Idle", to);
        }

        [Test]
        public void PopTo_UnregisteredState_ReturnsFalse()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            var result = _fsm.PopTo("NotExist");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void PopTo_TargetNotInStack_ReturnsFalse()
        {
            // Arrange
            RegisterStates("Idle", "Menu", "Settings");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            var result = _fsm.PopTo("Settings");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void PopTo_TargetAtTop_ReturnsTrueNoChange()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            var result = _fsm.PopTo("Menu");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, _fsm.StackDepth);
            Assert.AreEqual("Menu", _fsm.CurrentState);
        }

        [Test]
        public void PopTo_TargetInMiddle_PopsAboveTarget()
        {
            // Arrange
            RegisterStates("Idle", "Menu", "Settings", "Help");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");
            _fsm.Push("Settings");
            _fsm.Push("Help");

            // Act
            var result = _fsm.PopTo("Menu");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, _fsm.StackDepth);
            Assert.AreEqual("Menu", _fsm.CurrentState);
        }

        [Test]
        public void PopTo_ExitsStatesAboveTarget()
        {
            // Arrange
            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            var settingsState = new StackTestState("Settings");
            var helpState = new StackTestState("Help");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.RegisterState(settingsState);
            _fsm.RegisterState(helpState);
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");
            _fsm.Push("Settings");
            _fsm.Push("Help");

            // Act
            _fsm.PopTo("Menu");

            // Assert
            Assert.IsTrue(helpState.ExitCalled);
            Assert.IsTrue(settingsState.ExitCalled);
            Assert.IsFalse(menuState.ExitCalled);
            Assert.IsTrue(menuState.ResumeCalled);
        }

        [Test]
        public void PopAll_EmptyStack_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _fsm.PopAll());
        }

        [Test]
        public void PopAll_SingleState_DoesNotThrow()
        {
            // Arrange
            RegisterStates("Idle");
            _fsm.ChangeState("Idle");

            // Act & Assert
            Assert.DoesNotThrow(() => _fsm.PopAll());
            Assert.AreEqual(1, _fsm.StackDepth);
        }

        [Test]
        public void PopAll_MultipleStates_PopsToBottom()
        {
            // Arrange
            RegisterStates("Idle", "Menu", "Settings");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");
            _fsm.Push("Settings");

            // Act
            _fsm.PopAll();

            // Assert
            Assert.AreEqual(1, _fsm.StackDepth);
            Assert.AreEqual("Idle", _fsm.CurrentState);
        }

        [Test]
        public void PopAll_ExitsAllExceptBottomAndResumesBottom()
        {
            // Arrange
            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            var settingsState = new StackTestState("Settings");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.RegisterState(settingsState);
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");
            _fsm.Push("Settings");

            // Act
            _fsm.PopAll();

            // Assert
            Assert.IsTrue(settingsState.ExitCalled);
            Assert.IsTrue(menuState.ExitCalled);
            Assert.IsFalse(idleState.ExitCalled);
            Assert.IsTrue(idleState.ResumeCalled);
        }

        [Test]
        public void Update_OnlyCallsOnUpdateOnTopState()
        {
            // Arrange
            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            _fsm.Update(0.016f);

            // Assert
            Assert.AreEqual(0f, idleState.LastUpdateDeltaTime, 0.0001f);
            Assert.AreEqual(0.016f, menuState.LastUpdateDeltaTime, 0.0001f);
        }

        [Test]
        public void FixedUpdate_OnlyCallsOnFixedUpdateOnTopState()
        {
            // Arrange
            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            _fsm.FixedUpdate(0.02f);

            // Assert
            Assert.AreEqual(0f, idleState.LastFixedUpdateDeltaTime, 0.0001f);
            Assert.AreEqual(0.02f, menuState.LastFixedUpdateDeltaTime, 0.0001f);
        }

        [Test]
        public void Update_EmptyStack_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _fsm.Update(0.016f));
        }

        [Test]
        public void GetStackSnapshot_ReturnsCorrectOrder()
        {
            // Arrange
            RegisterStates("Idle", "Menu", "Settings");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");
            _fsm.Push("Settings");

            // Act
            var snapshot = _fsm.GetStackSnapshot();

            // Assert
            Assert.AreEqual(3, snapshot.Count);
            Assert.AreEqual("Idle", snapshot[0]);
            Assert.AreEqual("Menu", snapshot[1]);
            Assert.AreEqual("Settings", snapshot[2]);
        }

        [Test]
        public void IsInStack_StateInStack_ReturnsTrue()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Assert
            Assert.IsTrue(_fsm.IsInStack("Idle"));
            Assert.IsTrue(_fsm.IsInStack("Menu"));
        }

        [Test]
        public void IsInStack_StateNotInStack_ReturnsFalse()
        {
            // Arrange
            RegisterStates("Idle", "Menu", "Settings");
            _fsm.ChangeState("Idle");

            // Assert
            Assert.IsFalse(_fsm.IsInStack("Settings"));
        }

        [Test]
        public void TryGetState_Registered_ReturnsTrue()
        {
            // Arrange
            var state = new StackTestState("Idle");
            _fsm.RegisterState(state);

            // Act
            var result = _fsm.TryGetState("Idle", out var found);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("Idle", found.Key);
        }

        [Test]
        public void TryGetState_Unregistered_ReturnsFalse()
        {
            Assert.IsFalse(_fsm.TryGetState("NotExist", out _));
        }

        [Test]
        public void StackDepth_Initial_IsZero()
        {
            Assert.AreEqual(0, _fsm.StackDepth);
        }

        [Test]
        public void Dispose_ExitsAllStatesInStack()
        {
            // Arrange
            var idleState = new StackTestState("Idle");
            var menuState = new StackTestState("Menu");
            var settingsState = new StackTestState("Settings");
            _fsm.RegisterState(idleState);
            _fsm.RegisterState(menuState);
            _fsm.RegisterState(settingsState);
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");
            _fsm.Push("Settings");

            // Act
            _fsm.Dispose();

            // Assert
            Assert.IsTrue(settingsState.ExitCalled);
            Assert.IsTrue(menuState.ExitCalled);
            Assert.IsTrue(idleState.ExitCalled);
        }

        [Test]
        public void Dispose_ClearsEverything()
        {
            // Arrange
            RegisterStates("Idle", "Menu");
            _fsm.ChangeState("Idle");
            _fsm.Push("Menu");

            // Act
            _fsm.Dispose();

            // Assert
            Assert.AreEqual(0, _fsm.StackDepth);
            Assert.IsFalse(_fsm.HasState("Idle"));
            Assert.IsFalse(_fsm.HasState("Menu"));
        }

        [Test]
        public void Dispose_ClearsStackStateBaseReferences()
        {
            // Arrange
            var state = new StackTestState("Idle");
            _fsm.RegisterState(state);

            // Act
            _fsm.Dispose();

            // Assert
            Assert.IsNull(state.GetStateMachine());
        }

        [Test]
        public void Dispose_DoubleDispose_DoesNotThrow()
        {
            // Arrange
            RegisterStates("Idle");
            _fsm.ChangeState("Idle");
            _fsm.Dispose();

            // Act & Assert
            Assert.DoesNotThrow(() => _fsm.Dispose());
        }

        [Test]
        public void Scenario_GameMenuNavigation()
        {
            // 模拟游戏菜单导航场景：MainMenu -> Settings -> AudioSettings -> 返回
            RegisterStates("MainMenu", "Settings", "AudioSettings");

            // 进入主菜单
            _fsm.ChangeState("MainMenu");
            Assert.AreEqual("MainMenu", _fsm.CurrentState);
            Assert.AreEqual(1, _fsm.StackDepth);

            // 打开设置（压栈）
            _fsm.Push("Settings");
            Assert.AreEqual("Settings", _fsm.CurrentState);
            Assert.AreEqual(2, _fsm.StackDepth);

            // 打开音频设置（压栈）
            _fsm.Push("AudioSettings");
            Assert.AreEqual("AudioSettings", _fsm.CurrentState);
            Assert.AreEqual(3, _fsm.StackDepth);

            // 返回设置
            _fsm.Pop();
            Assert.AreEqual("Settings", _fsm.CurrentState);
            Assert.AreEqual(2, _fsm.StackDepth);

            // 返回主菜单
            _fsm.Pop();
            Assert.AreEqual("MainMenu", _fsm.CurrentState);
            Assert.AreEqual(1, _fsm.StackDepth);
        }

        [Test]
        public void Scenario_PushAndPopAll()
        {
            // 模拟深层导航后一键回到根部
            RegisterStates("Game", "Pause", "Inventory", "ItemDetail");
            _fsm.ChangeState("Game");
            _fsm.Push("Pause");
            _fsm.Push("Inventory");
            _fsm.Push("ItemDetail");

            Assert.AreEqual(4, _fsm.StackDepth);

            // 一键回到游戏
            _fsm.PopAll();

            Assert.AreEqual(1, _fsm.StackDepth);
            Assert.AreEqual("Game", _fsm.CurrentState);
        }

        [Test]
        public void Scenario_PopToSpecificState()
        {
            // 模拟深层导航后跳回指定层级
            RegisterStates("Root", "A", "B", "C", "D");
            _fsm.ChangeState("Root");
            _fsm.Push("A");
            _fsm.Push("B");
            _fsm.Push("C");
            _fsm.Push("D");

            // 从 D 直接回到 A（跳过 B、C）
            _fsm.PopTo("A");

            Assert.AreEqual(2, _fsm.StackDepth);
            Assert.AreEqual("A", _fsm.CurrentState);

            var snapshot = _fsm.GetStackSnapshot();
            Assert.AreEqual("Root", snapshot[0]);
            Assert.AreEqual("A", snapshot[1]);
        }

        [Test]
        public void Scenario_ChangeStateDoesNotGrowStack()
        {
            // ChangeState 应该替换栈顶，而非压栈
            RegisterStates("Idle", "Run", "Jump");
            _fsm.ChangeState("Idle");

            _fsm.ChangeState("Run");
            Assert.AreEqual(1, _fsm.StackDepth);

            _fsm.ChangeState("Jump");
            Assert.AreEqual(1, _fsm.StackDepth);
            Assert.AreEqual("Jump", _fsm.CurrentState);
        }

        [Test]
        public void Scenario_MixedChangeAndPush()
        {
            // 混合使用 ChangeState 和 Push
            RegisterStates("Game", "Menu", "Settings", "Dialog");
            _fsm.ChangeState("Game"); // 栈: [Game]
            _fsm.Push("Menu"); // 栈: [Game, Menu]
            _fsm.ChangeState("Dialog"); // 栈: [Game, Dialog]（替换 Menu）
            _fsm.Push("Settings"); // 栈: [Game, Dialog, Settings]

            Assert.AreEqual(3, _fsm.StackDepth);
            Assert.AreEqual("Settings", _fsm.CurrentState);

            var snapshot = _fsm.GetStackSnapshot();
            Assert.AreEqual("Game", snapshot[0]);
            Assert.AreEqual("Dialog", snapshot[1]);
            Assert.AreEqual("Settings", snapshot[2]);
        }

        private void RegisterStates(params string[] keys)
        {
            foreach (var key in keys) _fsm.RegisterState(new StackTestState(key));
        }

        /// <summary>
        ///     不实现 IStackState 的最小状态，用于测试类型校验
        /// </summary>
        private class NonStackState : IState<string>
        {
            public NonStackState(string key)
            {
                Key = key;
            }

            public string Key { get; }
        }

        /// <summary>
        ///     完整功能的栈测试状态
        /// </summary>
        private class StackTestState : StackStateBase<string>, IStateEnter, IStateExit, IStateUpdate, IStateFixedUpdate
        {
            public StackTestState(string key) : base(key)
            {
            }

            public bool EnterCalled { get; private set; }
            public bool ExitCalled { get; private set; }
            public bool PauseCalled { get; private set; }
            public bool ResumeCalled { get; private set; }
            public float LastUpdateDeltaTime { get; private set; }
            public float LastFixedUpdateDeltaTime { get; private set; }

            public override void OnEnter()
            {
                EnterCalled = true;
            }

            public override void OnExit()
            {
                ExitCalled = true;
            }

            public override void OnFixedUpdate(float fixedDeltaTime)
            {
                LastFixedUpdateDeltaTime = fixedDeltaTime;
            }

            public override void OnUpdate(float deltaTime)
            {
                LastUpdateDeltaTime = deltaTime;
            }

            public IStateMachineStack<string> GetStateMachine()
            {
                return StateMachine;
            }

            public override void OnPause()
            {
                base.OnPause();
                PauseCalled = true;
            }

            public override void OnResume()
            {
                base.OnResume();
                ResumeCalled = true;
            }
        }
    }
}