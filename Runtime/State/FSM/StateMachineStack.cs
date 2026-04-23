using System;
using System.Collections.Generic;

namespace CFramework
{
    /// <summary>
    ///     支持栈操作的状态机实现
    /// </summary>
    /// <typeparam name="TKey">状态键类型</typeparam>
    public sealed class StateMachineStack<TKey> : IStateMachineStack<TKey>
    {
        private readonly Dictionary<TKey, IStackState<TKey>> _states = new();
        private readonly List<IStackState<TKey>> _stateStack = new();
        private bool _isDisposed;

        /// <summary>
        ///     当前状态键
        /// </summary>
        public TKey CurrentState => _stateStack.Count > 0 ? _stateStack[^1].Key : default;

        /// <summary>
        ///     之前状态键
        /// </summary>
        public TKey PreviousState { get; private set; }

        /// <summary>
        ///     当前是否处于状态切换中
        /// </summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>
        ///     状态栈深度
        /// </summary>
        public int StackDepth => _stateStack.Count;

        /// <summary>
        ///     状态切换事件
        /// </summary>
        public event Action<TKey, TKey> OnStateChanged;

        /// <summary>
        ///     栈变化事件
        /// </summary>
        public event Action<IReadOnlyList<TKey>> OnStackChanged;

        /// <summary>
        ///     注册状态
        /// </summary>
        /// <param name="state">状态实例</param>
        public void RegisterState(IState<TKey> state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            if (state is not IStackState<TKey> stackState)
                throw new ArgumentException($"State must implement IStackState<TKey>. Type: {state.GetType().Name}");

            if (_states.ContainsKey(state.Key))
                throw new ArgumentException($"State with key '{state.Key}' already registered.");

            _states[stackState.Key] = stackState;

            // 如果是 StackStateBase，设置状态机引用
            if (stackState is StackStateBase<TKey> stateBase) stateBase.SetStateMachine(this);
        }

        /// <summary>
        ///     注销状态
        /// </summary>
        /// <param name="key">状态键</param>
        /// <returns>是否注销成功</returns>
        public bool UnregisterState(TKey key)
        {
            if (!_states.TryGetValue(key, out var state)) return false;

            // 如果状态在栈中，不允许注销
            if (IsInStack(key)) return false;

            // 清理状态机引用
            if (state is StackStateBase<TKey> stateBase) stateBase.SetStateMachine(null);

            return _states.Remove(key);
        }

        /// <summary>
        ///     切换状态（替换栈顶）
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否切换成功</returns>
        public bool ChangeState(TKey key)
        {
            if (!_states.TryGetValue(key, out var newState)) return false;

            if (IsTransitioning) return false;

            IsTransitioning = true;

            try
            {
                // 记录之前状态
                PreviousState = CurrentState;

                // 如果栈中有状态，退出栈顶
                if (_stateStack.Count > 0)
                {
                    var oldTop = _stateStack[^1];
                    if (oldTop is IStateExit stateExit)
                        stateExit.OnExit();
                    _stateStack.RemoveAt(_stateStack.Count - 1);
                }

                // 新状态入栈
                _stateStack.Add(newState);
                if (newState is IStateEnter stateEnter)
                    stateEnter.OnEnter();

                // 触发事件
                OnStateChanged?.Invoke(PreviousState, key);
                OnStackChanged?.Invoke(GetStackSnapshot());
            }
            finally
            {
                IsTransitioning = false;
            }

            return true;
        }

        /// <summary>
        ///     尝试切换状态
        /// </summary>
        public bool TryChangeState(TKey key)
        {
            try
            {
                return ChangeState(key);
            }
            catch (Exception ex)
            {
                LogUtility.Warning("StateMachineStack",
                    $"TryChangeState failed: {key}, Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     压入状态（当前状态暂停）
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否压入成功</returns>
        public bool Push(TKey key)
        {
            if (!_states.TryGetValue(key, out var newState)) return false;

            // 如果状态已在栈顶，不重复压入
            if (_stateStack.Count > 0 && _stateStack[^1].Key.Equals(key)) return false;

            if (IsTransitioning) return false;

            IsTransitioning = true;

            try
            {
                // 暂停当前栈顶状态
                if (_stateStack.Count > 0) _stateStack[^1].OnPause();

                PreviousState = CurrentState;

                // 新状态入栈
                _stateStack.Add(newState);
                if (newState is IStateEnter stateEnter)
                    stateEnter.OnEnter();

                // 触发事件
                OnStateChanged?.Invoke(PreviousState, key);
                OnStackChanged?.Invoke(GetStackSnapshot());
            }
            finally
            {
                IsTransitioning = false;
            }

            return true;
        }

        /// <summary>
        ///     弹出栈顶状态
        /// </summary>
        /// <returns>是否弹出成功</returns>
        public bool Pop()
        {
            if (_stateStack.Count <= 1)
                // 栈为空或只有一个状态时，不允许弹出
                return false;

            if (IsTransitioning) return false;

            IsTransitioning = true;

            try
            {
                // 退出栈顶状态
                var oldTop = _stateStack[^1];
                if (oldTop is IStateExit stateExit)
                    stateExit.OnExit();
                _stateStack.RemoveAt(_stateStack.Count - 1);

                PreviousState = oldTop.Key;

                // 恢复新的栈顶状态
                var newTop = _stateStack[^1];
                newTop.OnResume();

                // 触发事件
                OnStateChanged?.Invoke(PreviousState, newTop.Key);
                OnStackChanged?.Invoke(GetStackSnapshot());
            }
            finally
            {
                IsTransitioning = false;
            }

            return true;
        }

        /// <summary>
        ///     弹出到指定状态
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否弹出成功</returns>
        public bool PopTo(TKey key)
        {
            if (!_states.ContainsKey(key)) return false;

            // 查找目标状态在栈中的位置
            var targetIndex = -1;
            for (var i = 0; i < _stateStack.Count; i++)
                if (_stateStack[i].Key.Equals(key))
                {
                    targetIndex = i;
                    break;
                }

            // 目标状态不在栈中
            if (targetIndex < 0) return false;

            // 目标状态已在栈顶
            if (targetIndex == _stateStack.Count - 1) return true;

            if (IsTransitioning) return false;

            IsTransitioning = true;

            try
            {
                // 退出目标状态之上的所有状态
                for (var i = _stateStack.Count - 1; i > targetIndex; i--)
                    if (_stateStack[i] is IStateExit stateExit)
                        stateExit.OnExit();

                // 移除状态
                var removeCount = _stateStack.Count - 1 - targetIndex;
                _stateStack.RemoveRange(targetIndex + 1, removeCount);

                PreviousState = default;

                // 恢复目标状态
                var targetState = _stateStack[targetIndex];
                targetState.OnResume();

                // 触发事件
                OnStateChanged?.Invoke(PreviousState, targetState.Key);
                OnStackChanged?.Invoke(GetStackSnapshot());
            }
            finally
            {
                IsTransitioning = false;
            }

            return true;
        }

        /// <summary>
        ///     弹出所有状态到栈底
        /// </summary>
        public void PopAll()
        {
            if (_stateStack.Count <= 1) return;

            if (IsTransitioning) return;

            IsTransitioning = true;

            try
            {
                // 从栈顶往下退出所有状态（保留栈底）
                for (var i = _stateStack.Count - 1; i >= 1; i--)
                    if (_stateStack[i] is IStateExit stateExit)
                        stateExit.OnExit();

                // 只保留栈底
                var bottomState = _stateStack[0];
                _stateStack.Clear();
                _stateStack.Add(bottomState);

                PreviousState = default;

                // 恢复栈底状态
                bottomState.OnResume();

                // 触发事件
                OnStateChanged?.Invoke(PreviousState, bottomState.Key);
                OnStackChanged?.Invoke(GetStackSnapshot());
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        /// <summary>
        ///     更新状态机
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public void Update(float deltaTime)
        {
            // 只更新栈顶状态
            if (_stateStack.Count > 0)
                if (_stateStack[^1] is IStateUpdate stateUpdate)
                    stateUpdate.OnUpdate(deltaTime);
        }

        /// <summary>
        ///     固定更新状态机
        /// </summary>
        /// <param name="fixedDeltaTime">固定帧间隔时间</param>
        public void FixedUpdate(float fixedDeltaTime)
        {
            // 只更新栈顶状态
            if (_stateStack.Count > 0)
                if (_stateStack[^1] is IStateFixedUpdate stateFixedUpdate)
                    stateFixedUpdate.OnFixedUpdate(fixedDeltaTime);
        }

        /// <summary>
        ///     检查状态是否存在
        /// </summary>
        public bool HasState(TKey key)
        {
            return _states.ContainsKey(key);
        }

        /// <summary>
        ///     获取状态实例
        /// </summary>
        public bool TryGetState(TKey key, out IState<TKey> state)
        {
            if (_states.TryGetValue(key, out var stackState))
            {
                state = stackState;
                return true;
            }

            state = null;
            return false;
        }

        /// <summary>
        ///     获取状态栈快照
        /// </summary>
        public IReadOnlyList<TKey> GetStackSnapshot()
        {
            var keys = new List<TKey>(_stateStack.Count);
            foreach (var state in _stateStack) keys.Add(state.Key);

            return keys;
        }

        /// <summary>
        ///     检查指定状态是否在栈中
        /// </summary>
        public bool IsInStack(TKey key)
        {
            foreach (var state in _stateStack)
                if (state.Key.Equals(key))
                    return true;

            return false;
        }

        /// <summary>
        ///     释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            // 从栈顶往下退出所有状态
            for (var i = _stateStack.Count - 1; i >= 0; i--)
                if (_stateStack[i] is IStateExit stateExit)
                    stateExit.OnExit();

            // 清理所有状态
            foreach (var state in _states.Values)
                if (state is StackStateBase<TKey> stateBase)
                    stateBase.SetStateMachine(null);

            _states.Clear();
            _stateStack.Clear();
            _isDisposed = true;
        }
    }
}