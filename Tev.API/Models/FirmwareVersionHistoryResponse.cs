using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class FirmwareVersionHistoryResponse
    {
        public string Version { get; set; }
        public long InstalledOn { get; set; }
    }
}
