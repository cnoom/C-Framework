using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     默认序列化器，基于 Unity JsonUtility
    ///     <para>限制：不支持 Dictionary、多态、null 字段、匿名类型</para>
    ///     <para>如需这些特性，请实现 ISaveSerializer 并注入 SaveService</para>
    /// </summary>
    public sealed class JsonUtilitySerializer : ISaveSerializer
    {
        public string Serialize<T>(T value)
        {
            return JsonUtility.ToJson(value, true);
        }

        public T Deserialize<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
