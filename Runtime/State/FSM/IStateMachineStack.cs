using System;
using System.Collections.Generic;

namespace CFramework
{
    /// <summary>
    ///     支持栈操作的状态机接口
    /// </summary>
    /// <typeparam name="TKey">状态键类型</typeparam>
    public interface IStateMachineStack<TKey> : IStateMachine<TKey>
    {
        /// <summary>
        ///     状态栈深度
        /// </summary>
        int StackDepth { get; }

        /// <summary>
        ///     栈变化事件
        /// </summary>
        event Action<IReadOnlyList<TKey>> OnStackChanged;

        /// <summary>
        ///     压入状态（当前状态暂停）
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否压入成功</returns>
        bool Push(TKey key);

        /// <summary>
        ///     弹出栈顶状态（恢复下方状态）
        /// </summary>
        /// <returns>是否弹出成功</returns>
        bool Pop();

        /// <summary>
        ///     弹出到指定状态
        /// </summary>
        /// <param name="key">目标状态键</param>
        /// <returns>是否弹出成功</returns>
        bool PopTo(TKey key);

        /// <summary>
        ///     弹出所有状态到栈底
        /// </summary>
        void PopAll();

        /// <summary>
        ///     获取状态栈快照
        /// </summary>
        IReadOnlyList<TKey> GetStackSnapshot();

        /// <summary>
        ///     检查指定状态是否在栈中
        /// </summary>
        /// <param name="key">状态键</param>
        bool IsInStack(TKey key);
    }
}