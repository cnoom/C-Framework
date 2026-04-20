namespace CFramework
{
    /// <summary>
    ///     配置数据来源
    /// </summary>
    public enum ConfigDataSource
    {
        /// <summary>
        ///     ScriptableObject 资产（当前已实现）
        /// </summary>
        ScriptableObject,

        /// <summary>
        ///     二进制数据（预留扩展）
        /// </summary>
        Binary,

        /// <summary>
        ///     JSON 数据（预留扩展）
        /// </summary>
        Json,

        /// <summary>
        ///     网络数据（预留扩展）
        /// </summary>
        Network,

        /// <summary>
        ///     外部注入数据（当前已实现，通过 SetData 注入）
        /// </summary>
        External
    }
}
