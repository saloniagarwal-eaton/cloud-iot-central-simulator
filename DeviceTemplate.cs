using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace iot_developer_dps_m1
{
    public class DeviceTemplate
    {
        [JsonProperty(PropertyName = "@id")]
        public string id { get; set; }

        [JsonProperty(PropertyName = "contents")]
        public List<Content> contents { get; set; }
    }

    public class Content
    {
        [JsonProperty(PropertyName = "@type")]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]
        public List<string> type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string name { get; set; }

        [JsonProperty(PropertyName = "schema")]
        public object schema { get; set; }

        public Schema schemaCasted
        {
            get
            {
                if (schema is string)
                    return null;
                else
                    return JsonConvert.DeserializeObject<Schema>(schema.ToString()); 
            }
        }
    }

    internal class SingleOrArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(List<T>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Array)
            {
                return token.ToObject<List<T>>();
            }
            return new List<T> { token.ToObject<T>() };
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
