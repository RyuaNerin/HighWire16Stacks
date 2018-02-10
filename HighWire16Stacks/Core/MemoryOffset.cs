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
            var rv = reader.Value;
            
            if (rv is string sv)
            {
                if (sv.StartsWith("0x"))
                    return Convert.ToInt32(sv.Substring(2), 16);
                else
                    return Convert.ToInt32(sv);
            }
            else if (rv is int iv)
            {
                return iv;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    [JsonObject]
    internal class MemoryOffset
    {
        [JsonProperty(PropertyName = "player_id_offset")]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int PtrPlayerId { get; set; }

        [JsonProperty(PropertyName = "ptr_player")]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int PtrPlayer { get; set; }

        [JsonProperty(PropertyName = "ptr_target")]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int PtrTarget { get; set; }

        [JsonProperty(PropertyName = "player_status_offset")]
        [JsonConverter(typeof(JsonHexToIntConverter))]
        public int StatusOffset { get; set; }

        [JsonProperty(PropertyName = "player_status_count")]
        public int StatusCount { get; set; }
    }
}
