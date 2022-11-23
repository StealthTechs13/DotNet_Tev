using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class UpdateDeviceVideoConfigurationRequest
    {
        public string VideoMethod { get; set; }

        public string Resolution { get; set; }
    }
    
}
