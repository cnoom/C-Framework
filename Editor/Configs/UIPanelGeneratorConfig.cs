namespace CFramework.Editor.Configs
{
    /// <summary>
    ///     UI 面板代码生成器配置
    /// </summary>
    public static class UIPanelGeneratorConfig
    {
        /// <summary>
        ///     命名空间
        /// </summary>
        public const string Namespace = "UI";

        /// <summary>
        ///     绑定代码输出目录（Assets 相对路径）
        /// </summary>
        public const string OutputPath = "Assets/Scripts/UI";

        /// <summary>
        ///     绑定文件后缀
        /// </summary>
        public const string BindingsFileSuffix = ".Bindings.cs";

        /// <summary>
        ///     用户文件后缀
        /// </summary>
        public const string UserFileSuffix = ".cs";

        /// <summary>
        ///     字段前缀
        /// </summary>
        public const string FieldPrefix = "_";

        /// <summary>
        ///     是否生成 XML 注释
        /// </summary>
        public const bool GenerateXmlComments = true;

        /// <summary>
        ///     是否生成用户骨架文件
        /// </summary>
        public const bool GenerateUserFile = true;
    }
}