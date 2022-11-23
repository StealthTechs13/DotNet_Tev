using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class UpdateDeviceRequest
    {
        /// <summary>
        /// If left blank, device name wont be considered for update
        /// </summary>
        [RegularExpression(@"^[a-zA-Z0-9\s''_-]+$", ErrorMessage = "The special characters are not allowed in Device Name")]
        public string DeviceName { get; set; }

        /// <summary>
        /// If left blank location wont be considered for update
        /// </summary>
        public string LocationId { get; set; }
    }
}
