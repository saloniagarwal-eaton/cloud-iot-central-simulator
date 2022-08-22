namespace iot_developer_dps_m1
{
    public class ProvisionPayload
    {
        public string modelId { get; set; }

        public IoTGateway iotcGateway { get; set; }
    }

    public class IoTGateway
    {
        public string iotcGatewayId { get; set; }
    }
}
