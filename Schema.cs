using Newtonsoft.Json;
using System.Collections.Generic;

namespace iot_developer_dps_m1
{
    public class DisplayName
    {
        public string en { get; set; }
    }

    public class EnumValue
    {
        [JsonProperty("@id")]
        public string Id { get; set; }
        public DisplayName displayName { get; set; }
        public string enumValue { get; set; }
        public string name { get; set; }
    }

    public class Schema
    {
        [JsonProperty("@id")]
        public string Id { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }
        public List<EnumValue> enumValues { get; set; }
    }
}
