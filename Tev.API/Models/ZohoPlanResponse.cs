using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class ZohoPlanResponse
    {
        public string PlanCode { get; set; }
        public string Name { get; set; }
        public string BillingMode { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int TrailPeriod { get; set; }
        public double RecurringPrice { get; set; }
        public string Unit { get; set; }
        public int Interval { get; set; }
        public string IntervalUnit { get; set; }
        public DateTime? CreatedTime { get; set; }
        public string CreatedTimeFormatted { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdateTimeFormatted { get; set; }
        public List<AddOn> AddOns { get; set; }
    }
    public class AddOn
    {
        public string Name { get; set; }
        public int AddOnCode { get; set; }
    }
   
}
