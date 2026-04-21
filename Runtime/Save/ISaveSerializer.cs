namespace CFramework
{
    /// <summary>
    ///     存档序列化器接口
    ///     <para>默认使用 Newtonsoft.Json 实现，支持 Dictionary、多态等复杂类型</para>
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
