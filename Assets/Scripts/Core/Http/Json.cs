using Newtonsoft.Json;

namespace LiteGame.Core.Http
{
    /// <summary>Newtonsoft 薄封装；DTO 字段名直映射，不做命名策略转换。</summary>
    public static class Json
    {
        public static string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public static T Deserialize<T>(string text)
        {
            return string.IsNullOrEmpty(text) ? default : JsonConvert.DeserializeObject<T>(text);
        }

        /// <summary>容错反序列化：空串或格式错返回 false，不抛。</summary>
        public static bool TryDeserialize<T>(string text, out T value)
        {
            value = default;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            try
            {
                value = JsonConvert.DeserializeObject<T>(text);
                return value != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
