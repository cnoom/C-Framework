namespace CFramework
{
    /// <summary>
    ///     配置数据项接口
    ///     实现此接口以定义配置数据的主键
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface IConfigItem<TKey>
    {
        /// <summary>
        ///     配置数据的主键
        /// </summary>
        TKey Key { get; }
    }
}