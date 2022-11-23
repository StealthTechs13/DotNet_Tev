using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class LiveStreamQueueModel
    {
        public string DeviceId { get; set; } 
        public string LogicalDeviceId { get; set; }

        [Required]
        public int OrgId { get; set; }
        public string DeviceName { get; set; }
        public string Message { get; set; }
        public int MessageCode { get; set; } = 0;
        public bool Retrying { get; set; } = false;
        public int RetryCount { get; set; } = 0;
        public int AzureFunctionRetryCount { get; set; } = 0;
        public long? AutoStopSequenceNumber { get; set; }
    }
}
