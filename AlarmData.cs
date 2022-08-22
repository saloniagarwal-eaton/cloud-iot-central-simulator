using System;
using System.Collections.Generic;
using System.Text;

namespace iot_developer_dps_m1
{
    class AlarmData
    {
        public int version { get; set; }

        public string alarm_id { get; set; }

        public string timestamp { get; set; }

        public string device_id { get; set; }

        public int channel_id { get; set; }

        public string channel_v { get; set; }

        public bool latching { get; set; }

        public bool closed { get; set; }

        public int ack_level { get; set; }

        public string note { get; set; }

        public string user { get; set; }

        public string source { get; set; }

        public bool condition_cleared { get; set; }

        public TriggerData trigger { get; set; }

        public customprop custom { get; set; }

    }

    public class TriggerData
    {
        public string trigger_id { get; set; }

        public int severity { get; set; }

        public int trigger_type { get; set; }

        public int priority { get; set; }

        public string threshold { get; set; }

    }

    public class customprop
    {
        public int wa_tenant_id { get; set; }

        public int wa_incident_id { get; set; }

        public string InvocationId { get; set; }
    }
}
