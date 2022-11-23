using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class ZohoRetrieveSubscriptionResponse
    {
        public string SubscriptionId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string CreatedDate { get; set; }
        public string  ActivatedDate { get; set; }
        public string ExpiryDate { get; set; }
        public int Interval { get; set; }
        public string IntervalUnit { get; set; }
        public string BillingMode { get; set; }
        public string ProductName { get; set; }
        public string ProductId { get; set; }
        public double SubTotal { get; set; }
        public double Amount { get; set; }
        public ZohoPlan Plan { get; set; }
        public List<ZohoAddon> AddOns { get; set; }
        public List<ZohoTax> Taxes { get; set; }

    }
    public class ZohoPlan
    {
        public string PlanCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string Quantity { get; set; }
    }
    public class ZohoAddon
    {
        public string AddOnCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string Quantity { get; set; }
    }
    public class ZohoTax
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Amount { get; set; }
    }
}

