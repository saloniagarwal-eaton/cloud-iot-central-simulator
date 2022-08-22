using Newtonsoft.Json;
using System.Collections.Generic;

namespace iot_developer_dps_m1
{
    public class DeviceTree
    {
        public string gatewayId { get; set; }

        [JsonProperty(PropertyName = "dtdlFileName")]
        public string fileName { get; set; }

        public List<ChildDevice> childDevices { get; set; }
    }

    public class ChildDevice
    {
        public string deviceId { get; set; }

        [JsonProperty(PropertyName = "dtdlFileName")]
        public string fileName { get; set; }
    }
}
