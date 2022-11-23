using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tev.IotHub.Models
{
    public class WSDTestData
    {
        [JsonProperty(PropertyName = "GTemperatureSensorOffset2")]
        public int? GTemperatureSensorOffset2 { get; set; }
        [JsonProperty(PropertyName = "GTemperatureSensorOffset")]
        public int? GTemperatureSensorOffset { get; set; }
        [JsonProperty(PropertyName = "ClearAir")]
        public int? ClearAir { get; set; }
        [JsonProperty(PropertyName = "IREDCalibration")]
        public int? IREDCalibration { get; set; }
        [JsonProperty(PropertyName = "PhotoOffset")]
        public int? PhotoOffset { get; set; }
        [JsonProperty(PropertyName = "DriftLimit")]
        public int? DriftLimit { get; set; }
        [JsonProperty(PropertyName = "DriftBypass")]
        public int? DriftBypass { get; set; }
        [JsonProperty(PropertyName = "TransmitResolution")]
        public int? TransmitResolution { get; set; }
        [JsonProperty(PropertyName = "TransmitThreshold")]
        public int? TransmitThreshold { get; set; }
        [JsonProperty(PropertyName = "SmokeThreshold")]
        public int? SmokeThreshold { get; set; }
        [JsonProperty(PropertyName = "TestId")]
        public int TestId { get; set; }

        
    }
}
