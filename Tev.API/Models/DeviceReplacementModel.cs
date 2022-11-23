using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    /// <summary>
    /// Device Replacement Model
    /// </summary>
    public class DeviceReplacementModel
    {
        [JsonProperty("deviceReplacementId")]
        public int DeviceReplacementId { get; set; }
        [Required(ErrorMessage = "Please Enter Device Id")]
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }
        [Required(ErrorMessage = "Please Enter Org Id")]
        [JsonProperty("orgId")]
        public string OrgId { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("address")]
        public string Comments { get; set; }
        [JsonProperty("replaceStatus")]
        public string ReplaceStatus { get; set; }
    }
}
