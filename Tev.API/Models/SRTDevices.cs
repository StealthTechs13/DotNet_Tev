using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class SRTDevices
    {
        public string _id { get; set; }
        public string type { get; set; }
        public string ip { get; set; }
        public string name { get; set; }
        public long lastConnectedAt { get; set; }
        public string statusCode { get; set; }
        public string status { get; set; }
        public string statusDetails { get; set; }
        public object serialNumber { get; set; }
        public string firmware { get; set; }
        public bool hasAdminError { get; set; }
        public bool pendingSync { get; set; }
        public string lastConnection { get; set; }
    }
}
