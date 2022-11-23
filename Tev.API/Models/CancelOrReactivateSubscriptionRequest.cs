using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class CancelOrReactivateSubscriptionRequest
    {
        /// <summary>
        /// Subscription id
        /// </summary>
        public string SubscriptionId { get; set; }
        /// <summary>
        /// Logical device id
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// Device name
        /// </summary>
        public string DeviceName { get; set; }
    }
}
