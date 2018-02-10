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
            var v = reader.Value.ToString();

            if (v.StartsWith("0x"))
                return Convert.ToInt32(v.Substring(2), 16);

            return Convert.ToInt32(v.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    [JsonObject]
    internal class MemoryOffset
    {
        [JsonProperty(PropertyName = "ptr_target")]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int PtrTarget { get; set; }

        [JsonProperty(PropertyName = "ptr_player")]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int PtrPlayer { get; set; }

        [JsonProperty(PropertyName = "ptr_player_id")]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int PtrPlayerId { get; set; }

        [JsonProperty(PropertyName = "player_status_offset")]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int StatusOffset { get; set; }

        [JsonProperty(PropertyName = "player_status_count")]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int StatusCount { get; set; }
    }
}
