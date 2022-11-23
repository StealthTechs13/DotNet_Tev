using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class AssigneDevicesToTechnicianModel
    {
        [JsonProperty("technicianId")]
        public string TechnicianId { get; set; }
        [JsonProperty("device")]
        public string TechnicianDevice { get; set; }
    }

}
