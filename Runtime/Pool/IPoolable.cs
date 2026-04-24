namespace CFramework
{
    /// <summary>
    ///     可池化对象接口
    ///     <para>实现此接口的对象在归还池时会自动调用 OnReturn</para>
    ///     <para>从池中获取时会自动调用 OnGet</para>
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        ///     从池中获取时调用，用于重置状态
        /// </summary>
        void OnGet();

        /// <summary>
        ///     归还池时调用，用于清理资源
        /// </summary>
        void OnReturn();
    }
}
