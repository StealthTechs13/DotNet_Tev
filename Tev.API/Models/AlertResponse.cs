using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Enums;

namespace Tev.API.Models
{
    public class AlertResponse
    {
        public string AlertId { get; set; }
        public string AlertType { get; set; }
        public string ImageUrl { get; set; } 
        [JsonProperty(PropertyName ="occurenceEpochTime")]
        public long OccurenceTimeStamp { get; set; }
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public string LocationName { get; set; }
        public string LocationId { get; set; }
        public bool Acknowledged { get; set; }
        public bool BookMarked { get; set; }
        public bool IsCorrect { get; set; }
        public string Comment { get; set; }
        public string VideoUrl { get; set; }
        public string Device { get; set; }
        public int? SmokeValue { get; set; }
        public string AlertOccurred { get; set; }

        public string AlertStatus { get; set; }

    }
}
