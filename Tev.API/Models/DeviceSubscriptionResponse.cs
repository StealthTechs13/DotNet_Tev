using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class DeviceSubscriptionResponse
    {
        public string SubsscriptionName { get; set; }

        /// <summary>
        /// Last payment made for this subscription in Epoch Time format
        /// </summary>
        public long LastPaidDate { get; set; }

        /// <summary>
        /// Next renewal date for this subscription in Epoch Time
        /// </summary>
        public long NextRenewalData { get; set; }

        public List<string> SubscribedAlerts { get; set; }
    }
}
