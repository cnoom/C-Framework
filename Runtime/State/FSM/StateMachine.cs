using System;
using System.Collections.Generic;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     状态机实现
    /// </summary>
    /// <typeparam name="TKey">状态键类型</typeparam>
    public sealed class StateMachine<TKey> : IStateMachine<TKey>
    {
        private readonly Dictionary<TKey, IState<TKey>> _states = new();
        private IState<TKey> _currentState;
        private bool _isDisposed;
        private IState<TKey> _previousState;

        /// <summary>
        ///     当前状态键
        /// </summary>
        public TKey CurrentState => _currentState != null ? _currentState.Key : default;

        /// <summary>
        ///     之前状态键
        /// </summary>
        public TKey PreviousState => _previousState != null ? _previousState.Key : default;

        /// <summary>
        ///     当前是否处于状态切换中
        /// </summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>
        ///     状态切换事件
        /// </summary>
        public event Action<TKey, TKey> OnStateChanged;

        /// <summary>
        ///     注册状态
        /// </summary>
        /// <param name="state">状态实例</param>
        public void RegisterState(IState<TKey> state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            if (_states.ContainsKey(state.Key))
                throw new ArgumentException($"State with key '{state.Key}' already registered.");

            _states[state.Key] = state;

            // 如果是 StateBase，设置状态机引用
            if (state is IStateMachineHolder<TKey> stateBase) stateBase.StateMachine = this;
        }

        /// <summary>
        ///     注销状态
        /// </summary>
        /// <param name="key">状态键</param>
        /// <returns>是否注销成功</returns>
        public bool UnregisterState(TKey key)
        {
            if (!_states.TryGetValue(key, out var state)) return false;

            // 如果正在运行该状态，不允许注销
            if (_currentState == state) return false;

            // 清理状态机引用
            if (state is IStateMachineHolder<TKey> stateBase) stateBase.StateMachine = null;

            return _states.Remove(key);
        }

        /// <summary>
        ///     切换状态
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否切换成功</returns>
        public bool ChangeState(TKey key)
        {
            if (!_states.TryGetValue(key, out var newState)) return false;

            // 如果已经是当前状态，不切换
            if (_currentState == newState) return false;

            // 不允许在切换过程中再次切换
            if (IsTransitioning) return false;

            IsTransitioning = true;

            try
            {
                // 退出当前状态
                if (_currentState != null && _currentState is IStateExit exitState)
                    exitState?.OnExit();

                // 记录之前状态
                _previousState = _currentState;

                // 切换到新状态
                _currentState = newState;

                // 进入新状态
                if (_currentState is IStateEnter enterState)
                    enterState.OnEnter();

                // 触发状态切换事件
                OnStateChanged?.Invoke(_previousState != null ? _previousState.Key : default, key);
            }
            finally
            {
                IsTransitioning = false;
            }

            return true;
        }

        /// <summary>
        ///     尝试切换状态（安全版本）
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否切换成功</returns>
        public bool TryChangeState(TKey key)
        {
            try
            {
                return ChangeState(key);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StateMachine] TryChangeState failed: {key}, Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     更新状态机
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public void Update(float deltaTime)
        {
            if (_currentState != null && _currentState is IStateUpdate updateState)
                updateState.OnUpdate(deltaTime);
        }

        /// <summary>
        ///     固定更新状态机
        /// </summary>
        /// <param name="fixedDeltaTime">固定帧间隔时间</param>
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (_currentState != null && _currentState is IStateFixedUpdate fixedUpdateState)
                fixedUpdateState.OnFixedUpdate(fixedDeltaTime);
        }

        /// <summary>
        ///     检查状态是否存在
        /// </summary>
        /// <param name="key">状态键</param>
        public bool HasState(TKey key)
        {
            return _states.ContainsKey(key);
        }

        /// <summary>
        ///     获取状态实例
        /// </summary>
        /// <param name="key">状态键</param>
        /// <param name="state">状态实例</param>
        /// <returns>是否获取成功</returns>
        public bool TryGetState(TKey key, out IState<TKey> state)
        {
            return _states.TryGetValue(key, out state);
        }

        /// <summary>
        ///     释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            // 退出当前状态
            if (_currentState != null && _currentState is IStateExit exitState)
                exitState.OnExit();

            // 清理所有状态
            foreach (var state in _states.Values)
                if (state is IStateMachineHolder<TKey> stateBase)
                    stateBase.StateMachine = null;

            _states.Clear();
            _currentState = null;
            _previousState = null;
            _isDisposed = true;
        }
    }
}