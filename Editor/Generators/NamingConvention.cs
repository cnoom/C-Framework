namespace CFramework.Editor.Generators
{
    /// <summary>
    ///     命名规则
    /// </summary>
    public enum NamingConvention
    {
        /// <summary>
        ///     帕斯卡命名法：BattleBgm
        /// </summary>
        PascalCase,

        /// <summary>
        ///     驼峰命名法：battleBgm
        /// </summary>
        CamelCase,

        /// <summary>
        ///     小写：battlebgm
        /// </summary>
        LowerCase,

        /// <summary>
        ///     保持原样
        /// </summary>
        Original
    }
}