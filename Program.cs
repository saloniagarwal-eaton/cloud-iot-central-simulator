using iot_developer_dps_m1;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iot_developer_devices_iot_central_m2
{
    class Program
    {
        private static string provisioningGlobalDeviceEndpoint = System.Configuration.ConfigurationManager.AppSettings["ProvisioningGlobalDeviceEndpoint"];
        private static string provisioningIdScope = System.Configuration.ConfigurationManager.AppSettings["ProvisioningIdScope"];
        private static bool useGroupSymmetricKey = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["UseGroupSymmetricKey"]);
        private static string groupSymmetricKey = System.Configuration.ConfigurationManager.AppSettings["GroupSymmetricKey"];
        private static string devicePrimaryKey = System.Configuration.ConfigurationManager.AppSettings["DevicePrimaryKey"];
        private static bool sendTelemetryData = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["SendTelemetryData"]);

        private static readonly ConsoleColor defaultConsoleForegroundColor = Console.ForegroundColor;
        private static readonly object lockObject = new object();
        private static readonly Random random = new Random();

        public static Dictionary<string, Tuple<DeviceRegistrationResult,DeviceTemplate>> registrationResult = new Dictionary<string, Tuple<DeviceRegistrationResult, DeviceTemplate>>();

        private static string[] events = { "Informational", "Warning", "Exception"};

        static async Task Main(string[] args)
        {
            ConsoleWriteLine("*** Press ENTER to start ***");
            Console.ReadLine();

            ConsoleWriteLine("*** Starting... ***");
            ConsoleWriteLine("*** Press ENTER to quit ***");
            ConsoleWriteLine();

            ConsoleWriteLine("Start: Read DeviceTree input from jsonfile and store it");
            Console.WriteLine("\rEnter the DeviceTree Json filename: (without extension) ");
            string fileName = string.Concat(Console.ReadLine(), ".json");
            var deviceTree = LoadJson(Path.Combine(Directory.GetCurrentDirectory(), fileName));
            ConsoleWriteLine("End: Read DeviceTree input from jsonfile and store it");

            await Task.Delay(1000);

            var _devicesToSimulate = deviceTree.childDevices.Count + 1;

            for(int i = 0; i < _devicesToSimulate; i++)
            {
                DeviceRegistrationResult deviceRegistrationResult;
                DeviceTemplate dt;

                string deviceId;
                if (i == 0)
                {
                    deviceId = deviceTree.gatewayId;
                    dt = LoadTemplate(Path.Combine(Directory.GetCurrentDirectory(),"DTDLFiles", deviceTree.fileName));

                    deviceRegistrationResult = await RegisterDevice(deviceTree.gatewayId, dt.id, null);
                    if (deviceRegistrationResult == null)
                    {
                        return;
                    }
                }
                else
                {
                    deviceId = deviceTree.childDevices[i - 1].deviceId;
                    dt = LoadTemplate(Path.Combine(Directory.GetCurrentDirectory(), "DTDLFiles", deviceTree.childDevices[i - 1].fileName));

                    deviceRegistrationResult = await RegisterDevice(deviceTree.childDevices[i-1].deviceId, dt.id, deviceTree.gatewayId);
                    if (deviceRegistrationResult == null)
                    {
                        return;
                    }
                }
                Tuple<DeviceRegistrationResult, DeviceTemplate> tuple = new Tuple<DeviceRegistrationResult, DeviceTemplate>(deviceRegistrationResult , dt);
                registrationResult.Add(deviceId, tuple);
            }

            await SendDataAsync(registrationResult);
        }

        public static DeviceTree LoadJson(string path)
        {
            DeviceTree dt;
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                dt = JsonConvert.DeserializeObject<DeviceTree>(json);
            }
            return dt;
        }

        public static DeviceTemplate LoadTemplate(string path)
        {
            DeviceTemplate dt;
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                dt = JsonConvert.DeserializeObject<DeviceTemplate>(json);
            }
            return dt;
        }

        #region Provisioning

        private static async Task<DeviceRegistrationResult> RegisterDevice(string deviceId, string modelId, string gatewayId)
        {
            try
            {
                ConsoleWriteLine($"Will register device {deviceId}...", ConsoleColor.White);

                string derivedKey;
                if (useGroupSymmetricKey)
                    derivedKey = ComputeDerivedSymmetricKey(Convert.FromBase64String(groupSymmetricKey), deviceId);
                else
                    derivedKey = devicePrimaryKey;

                // using symmetric keys
                SecurityProvider symmetricKeyProvider = new SecurityProviderSymmetricKey(deviceId, derivedKey, null);
                ProvisioningTransportHandler mqttTransportHandler = new ProvisioningTransportHandlerMqtt();
                ProvisioningDeviceClient provisioningDeviceClient = ProvisioningDeviceClient.Create(provisioningGlobalDeviceEndpoint, provisioningIdScope,
                    symmetricKeyProvider, mqttTransportHandler);

                var pnpPayload = GetProvisioningPayload(modelId, gatewayId);

                DeviceRegistrationResult deviceRegistrationResult;
                if (pnpPayload == null)
                    deviceRegistrationResult = await provisioningDeviceClient.RegisterAsync();
                else
                    deviceRegistrationResult = await provisioningDeviceClient.RegisterAsync(pnpPayload);

                ConsoleWriteLine($"Device {deviceId} registration result: {deviceRegistrationResult.Status}", ConsoleColor.White);

                if (deviceRegistrationResult.Status != ProvisioningRegistrationStatusType.Assigned)
                {
                    throw new Exception($"Failed to register device {deviceId}");
                }

                ConsoleWriteLine($"Device {deviceId} was assigned to hub '{deviceRegistrationResult.AssignedHub}'", ConsoleColor.White);
                ConsoleWriteLine();

                return deviceRegistrationResult;
            }
            catch (Exception ex)
            {
            ConsoleWriteLine($"* ERROR * {ex.Message}", ConsoleColor.Red);
            }

            return null;
        }

        private static string ComputeDerivedSymmetricKey(byte[] masterKey, string registrationId)
        {
            using (var hmac = new HMACSHA256(masterKey))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
            }
        }

        private static ProvisioningRegistrationAdditionalData GetProvisioningPayload(string modelId, string gatewayId)
        {
            IoTGateway gatewayy = new IoTGateway();
            ProvisionPayload payload = new ProvisionPayload();

            if (modelId == null && gatewayId == null)
                return null;
            else if (modelId != null && gatewayId != null)
            {
                gatewayy.iotcGatewayId = gatewayId;
                payload.modelId = modelId;
                payload.iotcGateway = gatewayy;
                var pnpPayload = new ProvisioningRegistrationAdditionalData
                {
                    JsonData = JsonConvert.SerializeObject(payload),
                };

                return pnpPayload;
            }
            else if (modelId != null && gatewayId == null)
            {
                payload.modelId = modelId;
                var pnpPayload = new ProvisioningRegistrationAdditionalData
                {
                    JsonData = JsonConvert.SerializeObject(payload),
                };

                return pnpPayload;
            }
            else
            {
                gatewayy.iotcGatewayId = gatewayId;
                payload.iotcGateway = gatewayy;
                var pnpPayload = new ProvisioningRegistrationAdditionalData
                {
                    JsonData = JsonConvert.SerializeObject(payload),
                };

                return pnpPayload;
            }
        }

        #endregion

        #region DeviceClient

        public static async Task SendDataAsync(Dictionary<string, Tuple<DeviceRegistrationResult, DeviceTemplate>> registrationResult)
        {
            try
            {
                var TaskList = new List<Task>();
                var slim = new SemaphoreSlim(30);

                foreach (var result in registrationResult)
                {
                    TaskList.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await slim.WaitAsync();
                            SendData(result.Key, result.Value.Item1, result.Value.Item2);
                        }
                        finally
                        {
                            slim.Release();
                        }
                    }));
                }

                try
                {
                    await Task.WhenAll(TaskList.ToArray());
                }

                catch (AggregateException ae)
                {
                    throw ae.Flatten();
                }

                Console.WriteLine("All the devices had successfully connected and sent data....\n Press enter to exit.");
                Console.ReadLine();

                Console.WriteLine("Exiting...");
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public static void SendData(string deviceId, DeviceRegistrationResult result, DeviceTemplate deviceTemplate)
        {
            using var deviceClient = NewDeviceClient(deviceId, result.AssignedHub);
            if (deviceClient == null)
            {
                return;
            }

            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var sendDeviceDataTask = SendDeviceDataUntilCancelled(deviceId, deviceClient, cancellationToken, deviceTemplate);

            Console.ReadLine();
            ConsoleWriteLine("Shutting down...");

            cancellationTokenSource.Cancel(); // request cancel
            sendDeviceDataTask.Wait(); // wait for cancel
        }

        private static DeviceClient NewDeviceClient(string deviceId, string assignedHub)
        {
            try
            {
                ConsoleWriteLine();
                ConsoleWriteLine($"Will create client for device {deviceId}...", ConsoleColor.Green);

                string derivedKey;
                if (useGroupSymmetricKey)
                    derivedKey = ComputeDerivedSymmetricKey(Convert.FromBase64String(groupSymmetricKey), deviceId);
                else
                    derivedKey = devicePrimaryKey;

                var authenticationMethod = new DeviceAuthenticationWithRegistrySymmetricKey(
                    deviceId: deviceId, 
                    key: derivedKey);

                var deviceClient = DeviceClient.Create(
                    hostname: assignedHub,
                    authenticationMethod: authenticationMethod,
                    transportType: TransportType.Mqtt_Tcp_Only);
                
                ConsoleWriteLine($"Successfully created client for device {deviceId}", ConsoleColor.Green);

                return deviceClient;
            }
            catch (Exception ex)
            {
                ConsoleWriteLine($"* ERROR * {ex.Message}", ConsoleColor.Red);
            }

            return null;
        }

        private static async Task SendDeviceDataUntilCancelled(string deviceId, DeviceClient deviceClient, CancellationToken cancellationToken, DeviceTemplate deviceTemplate)
        {
            try
            {
                ConsoleWriteLine($"Start: Send message for device {deviceId}", ConsoleColor.White);
                while (!cancellationToken.IsCancellationRequested)
                {
                    Message message;
                    if (sendTelemetryData)
                        message = GetTelemetryEventMessage(deviceTemplate);
                    else
                        message = GetAlarmEventMessage(deviceId);

                    ConsoleColor? consoleColor = null;

                    // send message
                    if (consoleColor.HasValue)
                    {
                        ConsoleWriteLine();
                        ConsoleWriteLine($"Will send message for device {deviceId}:", consoleColor);
                    }

                    await deviceClient.SendEventAsync(message);

                    if (consoleColor.HasValue)
                    {
                        ConsoleWriteLine($"Successfully sent message for device {deviceId}", consoleColor);
                    }

                    await Task.Delay(2000);
                }
                ConsoleWriteLine($"End: Send message for device {deviceId}", ConsoleColor.White);
            }
            catch (Exception ex)
            {
                ConsoleWriteLine($"* ERROR * {ex.Message}", ConsoleColor.Red);
            }
        }

        #endregion

        #region EventMessage

        private static Message GetTelemetryEventMessage(DeviceTemplate deviceTemplate)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            foreach (var content in deviceTemplate.contents)
            {
                if (content.type.Contains("Telemetry"))
                {
                    if (content.type.Contains("Event") && content.schema is string)
                    {
                        attributes.Add(content.name, events[random.Next(events.Length)]);
                    }
                    else if (content.type.Contains("State"))
                    {
                        var enumvalues = random.Next(content.schemaCasted.enumValues.Count);
                        attributes.Add(content.name, content.schemaCasted.enumValues[enumvalues].enumValue);
                    }
                    else
                    {
                        var type = content.schema.ToString();
                        switch (type.ToLower())
                        {
                            case "double":
                                attributes.Add(content.name, random.Next(20, 90) + random.NextDouble());
                                break;
                            case "integer":
                                attributes.Add(content.name, random.Next(20, 90));
                                break;
                        }
                    }
                }
            }

            dynamic eo = attributes.Aggregate(new ExpandoObject() as IDictionary<string, Object>,
                            (a, p) => { a.Add(p.Key, p.Value); return a; });

            var bodyJson = JsonConvert.SerializeObject(eo, Formatting.Indented);
            var message = new Message(Encoding.UTF8.GetBytes(bodyJson))
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };

            return message;
        }

        private static Message GetAlarmEventMessage(string deviceId)
        {
            var msg = SendAlarmData(deviceId);

            var jsonData = JsonConvert.SerializeObject(msg, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var payload = new
            {
                Alarm = jsonData,
            };

            var bodyJson = JsonConvert.SerializeObject(payload, Formatting.Indented);
            var message = new Message(Encoding.UTF8.GetBytes(bodyJson))
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };
            message.Properties.Add("a", "alarm");
            message.Properties.Add("p", deviceId);

            return message;
        }

        private static AlarmData SendAlarmData(string deviceId)
        {
            AlarmData alarmData = new AlarmData();

            alarmData.timestamp = DateTime.UtcNow.ToString("o");
            alarmData.device_id = deviceId;
            TriggerData trigger = new TriggerData() { trigger_id = Guid.NewGuid().ToString() };
            alarmData.trigger = trigger;

            return alarmData;
        }

        #endregion

        #region Utility

        private static void ConsoleWriteLine(string message = null, ConsoleColor? foregroundColor = null)
        {
            lock (lockObject)
            {
            Console.ForegroundColor = foregroundColor ?? defaultConsoleForegroundColor;
            Console.WriteLine(message);
            Console.ForegroundColor = defaultConsoleForegroundColor;
            }
        }

        #endregion
    }
}
