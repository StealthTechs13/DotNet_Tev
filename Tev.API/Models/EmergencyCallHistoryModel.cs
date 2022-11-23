using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    /// <summary>
    /// Emergency Call History Model
    /// </summary>
    public class EmergencyCallHistoryModel
    {
        
        [JsonProperty("emergencyCallHistoryId")]
        public string EmergencyCallHistoryId { get; set; }
        [Required(ErrorMessage = "Please Enter Number")]
        [JsonProperty("number")]
        public string Number { get; set; }
        [JsonProperty("time")]
        public DateTime Time { get; set; }
        [JsonProperty("callingPurpose")]
        public string CallingPurpose { get; set; }
        [Required(ErrorMessage = "Please Enter DeviceId")]
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }
    }
}
