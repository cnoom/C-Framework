namespace CFramework
{
    /// <summary>
    ///     栈状态基类，提供便捷实现
    /// </summary>
    /// <typeparam name="TKey">状态键类型</typeparam>
    public abstract class StackStateBase<TKey> : IStackState<TKey>
    {
        protected StackStateBase(TKey key)
        {
            Key = key;
        }

        /// <summary>
        ///     所属状态机
        /// </summary>
        protected IStateMachineStack<TKey> StateMachine { get; private set; }

        /// <summary>
        ///     当前是否处于暂停状态
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     状态键
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        ///     状态被暂停（有新状态压栈）
        /// </summary>
        public virtual void OnPause()
        {
            IsPaused = true;
        }

        /// <summary>
        ///     状态被恢复（栈顶状态弹出）
        /// </summary>
        public virtual void OnResume()
        {
            IsPaused = false;
        }

        /// <summary>
        ///     设置所属状态机（内部使用）
        /// </summary>
        internal void SetStateMachine(IStateMachineStack<TKey> stateMachine)
        {
            StateMachine = stateMachine;
        }

        /// <summary>
        ///     进入状态
        /// </summary>
        public virtual void OnEnter()
        {
        }

        /// <summary>
        ///     退出状态
        /// </summary>
        public virtual void OnExit()
        {
        }

        /// <summary>
        ///     状态更新
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public virtual void OnUpdate(float deltaTime)
        {
        }

        /// <summary>
        ///     固定更新
        /// </summary>
        /// <param name="fixedDeltaTime">固定帧间隔时间</param>
        public virtual void OnFixedUpdate(float fixedDeltaTime)
        {
        }

        /// <summary>
        ///     压入新状态
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否压入成功</returns>
        protected bool Push(TKey key)
        {
            return StateMachine != null && StateMachine.Push(key);
        }

        /// <summary>
        ///     弹出当前状态
        /// </summary>
        /// <returns>是否弹出成功</returns>
        protected bool Pop()
        {
            return StateMachine != null && StateMachine.Pop();
        }

        /// <summary>
        ///     弹出到指定状态
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否弹出成功</returns>
        protected bool PopTo(TKey key)
        {
            return StateMachine != null && StateMachine.PopTo(key);
        }
    }
}