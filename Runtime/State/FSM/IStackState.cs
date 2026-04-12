namespace CFramework
{
    /// <summary>
    ///     支持栈操作的状态接口
    /// </summary>
    /// <typeparam name="TKey">状态键类型</typeparam>
    public interface IStackState<TKey> : IState<TKey>
    {
        /// <summary>
        ///     状态被暂停（有新状态压栈）
        /// </summary>
        void OnPause();

        /// <summary>
        ///     状态被恢复（栈顶状态弹出）
        /// </summary>
        void OnResume();
    }
}