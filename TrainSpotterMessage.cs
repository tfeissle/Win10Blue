using System;
using System.Net;
using System.Collections.Generic;

using Newtonsoft.Json;


//Sample Data:

//[{"Train Beacon":"10:94:65:72:2F:5F","Tx":0,"RSSI":-82,"Temperature":0.0,"Battery":0,"Uptime":0,"Packets Sent":0,"Distance":0,"URL":null,"Namespace":null,"Name":null,"MAC":"10:94:65:72:2F:5F","EID":null,"LastUpdate":"2018-02-12T19:13:06.9725325","BeaconType":"Unknown","TS":null,"EventProcessedUtcTime":"2018-02-12T19:14:02.1807105Z","PartitionId":0,"EventEnqueuedUtcTime":"2018-02-12T19:13:07.3160000Z","IoTHub":{"MessageId":null,"CorrelationId":null,"ConnectionDeviceId":"ajfRasp3","ConnectionDeviceGenerationId":"636157889864580597","EnqueuedTime":"2018-02-12T19:13:07.0560000Z","StreamId":null}}]

namespace TrainSpotter
{


    sealed public partial class TrainSpotterMessage
    {
        [JsonProperty("Train Beacon")]
        public string TrainBeacon { get; set; }

        [JsonProperty("Tx")]
        public long Tx { get; set; }

        [JsonProperty("RSSI")]
        public long Rssi { get; set; }

        [JsonProperty("Temperature")]
        public float Temperature { get; set; }

        [JsonProperty("Battery")]
        public long Battery { get; set; }

        [JsonProperty("Uptime")]
        public uint Uptime { get; set; }

        [JsonProperty("Packets Sent")]
        public uint PacketsSent { get; set; }

        [JsonProperty("Distance")]
        public int Distance { get; set; }

        [JsonProperty("URL")]
        public string Url { get; set; }

        [JsonProperty("Namespace")]
        public string Namespace { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("MAC")]
        public string Mac { get; set; }

        [JsonProperty("EID")]
        public string Eid { get; set; }

        [JsonProperty("LastUpdate")]
        public DateTimeOffset LastUpdate { get; set; }

        [JsonProperty("BeaconType")]
        public string BeaconType { get; set; }

        [JsonProperty("TS")]
        public Ts Ts { get; set; }
    }

    sealed public partial class Ts
    {
        [JsonProperty("$date")]
        public DateTimeOffset Date { get; set; }
    }

    sealed public partial class TrainSpotterMessage
    {

        public static TrainSpotterMessage FromJson(string json) => JsonConvert.DeserializeObject<TrainSpotterMessage>(json);
    }

    public static class Serialize
    {

        public static string ToJson(this TrainSpotterMessage self) => JsonConvert.SerializeObject(self);

    }
}

   

