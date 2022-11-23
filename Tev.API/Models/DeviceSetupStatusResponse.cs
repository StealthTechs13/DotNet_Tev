using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.API.Models
{
    public class DeviceSetupStatusResponse
    {
        public string LogicalDeviceId { get; set; }
        public string Message { get; set; }
        public Status Status { get; set; }
        public bool? Retrying { get; set; }
        public int? RetryCount { get; set; }
        public string DeviceName { get; set; }
        public int MessageCode { get; set; }
    }
}
