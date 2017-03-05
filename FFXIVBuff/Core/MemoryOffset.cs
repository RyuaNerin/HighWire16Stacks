using System;
using Newtonsoft.Json;

namespace FFXIVBuff.Core
{
    internal class JsonHexToInt : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var v = reader.ReadAsString();
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
        [JsonProperty(ItemConverterType=typeof(JsonHexToInt))]
        public int ptr { get; set; }

        [JsonProperty(ItemConverterType=typeof(JsonHexToInt))]
        public int off { get; set; }
    }

    [JsonObject]
    internal class MemoryOffsets
    {
        [JsonProperty]
        public MemoryOffset x86 { get; set; }

        [JsonProperty]
        public MemoryOffset x64 { get; set; }

        [JsonProperty]
        public int count { get; set; }
    }
}
