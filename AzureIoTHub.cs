using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;

namespace TrainSpotter
{
    class AzureIoTHub
    {

        private TrainSpotterMessage _trainSpotterMessage = new TrainSpotterMessage();
        private static void CreateClient()
        {
            if (deviceClient == null)
            {
                // create Azure IoT Hub client from embedded connection string
                deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
                Analytics.TrackEvent("CreateClient", new Dictionary<string, string> {
                           { "deviceClient", "D2C:" + deviceClient.ToString() },
                           { "Function", "CreateClient"}
                        });
            }
        }

        static DeviceClient deviceClient = null;

        //
        // Note: this connection string is specific to the device 
        // see information on iothub-explorer at http://aka.ms/iothubgetstartedVSCS
        //
        const string deviceConnectionString = "";
        private const string connectionString = "";




        // Refer to http://aka.ms/azure-iot-hub-vs-cs-2017-wiki for more information on Connected Service for Azure IoT Hub


        public static async Task SendDeviceToCloudMessageAsync(TrainSpotterMessage trainData)
        {
            CreateClient();

            var btData = trainData;
            string json = JsonConvert.SerializeObject(btData, Formatting.Indented);

            var message = new Message(Encoding.ASCII.GetBytes(json));

            await deviceClient.SendEventAsync(message);

            Analytics.TrackEvent("SendDeviceToCloudMessageAsync", new Dictionary<string, string> {
                           { "Category", "D2C:" + trainData.LastUpdate + ", " + trainData.Name + ", " + trainData.Url},
                           { "Function", "SendD2C"}
                        });

        }

        public static async Task<string> ReceiveCloudToDeviceMessageAsync()
        {
            CreateClient();

            while (true)
            {
                var receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    await deviceClient.CompleteAsync(receivedMessage);
                    return messageData;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static async Task<MethodResponse> OnSampleMethod1Called(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("SampleMethod1 has been called");
            return new MethodResponse(200);
        }

        private static async Task<MethodResponse> OnSampleMethod2Called(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("SampleMethod2 has been called");
            return new MethodResponse(200);
        }

        public static async Task RegisterDirectMethodsAsync()
        {
            CreateClient();

            Console.WriteLine("Registering direct method callbacks");
            await deviceClient.SetMethodHandlerAsync("SampleMethod1", OnSampleMethod1Called, null);
            await deviceClient.SetMethodHandlerAsync("SampleMethod2", OnSampleMethod2Called, null);
        }

        public static async Task GetDeviceTwinAsync()
        {
            CreateClient();

            Console.WriteLine("Getting device twin");
            Twin twin = await deviceClient.GetTwinAsync();
            Console.WriteLine(twin.ToJson());
        }

        private static async Task OnDesiredPropertiesUpdated(TwinCollection desiredProperties, object userContext)
        {
            Console.WriteLine("Desired properties were updated");
            Console.WriteLine(desiredProperties.ToJson());
        }

        public static async Task RegisterTwinUpdateAsync()
        {
            CreateClient();

            Console.WriteLine("Registering Device Twin update callback");
            await deviceClient.SetDesiredPropertyUpdateCallback(OnDesiredPropertiesUpdated, null);
        }

        public static async Task UpdateDeviceTwin()
        {
            CreateClient();

            TwinCollection tc = new TwinCollection();
            tc["SampleProperty1"] = "test value";

            Console.WriteLine("Updating Device Twin reported properties");
            await deviceClient.UpdateReportedPropertiesAsync(tc);
        }
    }
}
