using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class DeviceReplacementRequest
    {
        public string DeviceId { get; set; }
        public string Comment { get; set; }
    }
}
