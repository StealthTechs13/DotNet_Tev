using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class OrphanSubscriptionResponse
    {
        public string OrphanSubscriptionId { get; set; }
        public string ProductName { get; set; }
        public string PlanName { get; set; }
        public string Status { get; set; }
        public string NextBillingAt { get; set; }
        public double Amount { get; set; }
        public List<AvailableFeatureResponse> features { get; set; }
        public long PauseDate { get; set; }
    }
    public class AvailableFeatureResponse
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }
}
