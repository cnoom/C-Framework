using Newtonsoft.Json;

namespace CFramework
{
    /// <summary>
    ///     Newtonsoft.Json 预设序列化配置
    ///     <para>统一管理项目中所有 JSON 序列化场景的设置，避免各处重复创建 JsonSerializerSettings</para>
    /// </summary>
    public static class JsonPresets
    {
        /// <summary>
        ///     配置表专用：支持多态（$type）、格式化输出
        ///     <para>适用于：CardConfig、CharacterConfig 等包含 [SerializeReference] 多态字段的配置数据</para>
        /// </summary>
        public static readonly JsonSerializerSettings Config = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        /// <summary>
        ///     存档专用：支持多态、格式化输出
        ///     <para>适用于：游戏存档数据序列化/反序列化</para>
        /// </summary>
        public static readonly JsonSerializerSettings Save = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        /// <summary>
        ///     通用 JSON：不含多态支持，适用于简单数据结构
        ///     <para>适用于：不含 [SerializeReference] 的普通配置表</para>
        /// </summary>
        public static readonly JsonSerializerSettings Plain = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
    }
}
