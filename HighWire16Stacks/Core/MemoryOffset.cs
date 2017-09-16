using System;
using Newtonsoft.Json;

namespace HighWire16Stacks.Core
{
    internal class JsonHexToIntConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var v = reader.Value as string;
            if (v == null)
                return null;

            if (v.StartsWith("0x"))
                v = v.Substring(2);

            return Convert.ToInt32(v, 16);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    [JsonObject]
    internal class MemoryOffset
    {
        [JsonProperty]
        public MemoryOffset x64 { get; set; }

        [JsonProperty]
        public int count { get; set; }

        [JsonProperty]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int ptr { get; set; }

        [JsonProperty]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int off { get; set; }
    }
}
