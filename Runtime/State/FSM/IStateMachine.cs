using System;

namespace CFramework
{
    /// <summary>
    ///     状态机接口
    /// </summary>
    /// <typeparam name="TKey">状态键类型</typeparam>
    public interface IStateMachine<TKey> : IDisposable
    {
        /// <summary>
        ///     当前状态键
        /// </summary>
        TKey CurrentState { get; }

        /// <summary>
        ///     之前状态键
        /// </summary>
        TKey PreviousState { get; }

        /// <summary>
        ///     当前是否处于状态切换中
        /// </summary>
        bool IsTransitioning { get; }

        /// <summary>
        ///     状态切换事件
        /// </summary>
        event Action<TKey, TKey> OnStateChanged;

        /// <summary>
        ///     注册状态
        /// </summary>
        /// <param name="state">状态实例</param>
        void RegisterState(IState<TKey> state);

        /// <summary>
        ///     注销状态
        /// </summary>
        /// <param name="key">状态键</param>
        bool UnregisterState(TKey key);

        /// <summary>
        ///     切换状态
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否切换成功</returns>
        bool ChangeState(TKey key);

        /// <summary>
        ///     尝试切换状态（安全版本）
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否切换成功</returns>
        bool TryChangeState(TKey key);

        /// <summary>
        ///     更新状态机
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        void Update(float deltaTime);

        /// <summary>
        ///     固定更新状态机
        /// </summary>
        /// <param name="fixedDeltaTime">固定帧间隔时间</param>
        void FixedUpdate(float fixedDeltaTime);

        /// <summary>
        ///     检查状态是否存在
        /// </summary>
        /// <param name="key">状态键</param>
        bool HasState(TKey key);

        /// <summary>
        ///     获取状态实例
        /// </summary>
        /// <param name="key">状态键</param>
        /// <param name="state">状态实例</param>
        /// <returns>是否获取成功</returns>
        bool TryGetState(TKey key, out IState<TKey> state);
    }
}