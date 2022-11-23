using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class DeviceTypeResponse
    {
        /// <summary>
        /// Device Type Name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Device Type Description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Long name of the device
        /// </summary>
        [JsonProperty("longName")]
        public string LongName { get; set; }
    }
}
