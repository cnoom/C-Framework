namespace CFramework
{
    /// <summary>
    ///     存档序列化器接口
    ///     <para>默认使用 JsonUtility 实现，不支持 Dictionary 等复杂嵌套类型</para>
    ///     <para>如需更强大的序列化能力，可替换为 Newtonsoft.Json 等实现</para>
    /// </summary>
    public interface ISaveSerializer
    {
        /// <summary>
        ///     序列化对象为 JSON 字符串
        /// </summary>
        string Serialize<T>(T value);

        /// <summary>
        ///     反序列化 JSON 字符串为对象
        /// </summary>
        T Deserialize<T>(string json);
    }
}
