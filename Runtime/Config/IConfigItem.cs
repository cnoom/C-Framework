namespace CFramework
{
    /// <summary>
    ///     配置数据项标记接口（无泛型）
    ///     <para>用于 ConfigService 中仅需要标记 TValue 的方法（LoadAsync/Unload/RegisterAddress）</para>
    /// </summary>
    public interface IConfigItem
    {
    }

    /// <summary>
    ///     配置数据项接口
    ///     <para>实现此接口以定义配置数据的主键</para>
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface IConfigItem<TKey> : IConfigItem
    {
        /// <summary>
        ///     配置数据的主键
        /// </summary>
        TKey Key { get; }
    }
}
