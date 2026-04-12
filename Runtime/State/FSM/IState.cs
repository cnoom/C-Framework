namespace CFramework
{
    /// <summary>
    ///     状态接口
    /// </summary>
    /// <typeparam name="TKey">状态键类型</typeparam>
    public interface IState<TKey>
    {
        /// <summary>
        ///     状态键
        /// </summary>
        TKey Key { get; }
    }
}