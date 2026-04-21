using Newtonsoft.Json;

namespace CFramework
{
    /// <summary>
    ///     默认序列化器，基于 Newtonsoft.Json
    ///     <para>支持 Dictionary、多态、null 字段等复杂类型</para>
    /// </summary>
    public sealed class JsonUtilitySerializer : ISaveSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonUtilitySerializer()
        {
            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };
        }

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
