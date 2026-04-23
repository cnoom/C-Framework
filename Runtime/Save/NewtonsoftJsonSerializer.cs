using Newtonsoft.Json;

namespace CFramework
{
    /// <summary>
    ///     默认序列化器，基于 Newtonsoft.Json
    ///     <para>支持 Dictionary、多态、null 字段、匿名类型等复杂序列化场景</para>
    /// </summary>
    public sealed class NewtonsoftJsonSerializer : ISaveSerializer
    {
        private readonly JsonSerializerSettings _settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, _settings);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
    }
}
