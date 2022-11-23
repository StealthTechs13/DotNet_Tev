using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Tev.DAL.Entities
{
    public class ZohoSubscriptionHistory:Entity
    {
        [Key]
        public int Id { get; set; }
        public string SubscriptionId { get; set; }
        public string ProductName { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public string EventType { get; set; }
        public string PlanCode { get; set; }
        public string PlanName { get; set; }
        public double PlanPrice { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// SubTotal excluding tax amount 
        /// </summary>
        public double SubTotal { get; set; }
        /// <summary>
        /// Amount including tax amount
        /// </summary>
        public double Amount { get; set; }
        public ICollection<FeatureSubscriptionAssociation> Features { get; set; }
        public string CGSTName { get; set; }
        public string SGSTName { get; set; }
        public double CGSTAmount { get; set; }
        public double SGSTAmount { get; set; }
        public int TaxPercentage { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string InvoiceId { get; set; }
        public int Interval { get; set; }
        public string IntervalUnit { get; set; }
        public string Currency { get; set; }
        public string OrgId { get; set; }
        public string NextBillingAt { get; set; }
        public string SubscriptionNumber { get; set; }

        public string PhysicalDeviceId { get; set; }

    }
    public class FeatureSubscriptionAssociation:Entity
    {
        [Key]
        [JsonIgnore]
        public string Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        [JsonIgnore]
        public int ZohoSubscriptionHistoryFK { get; set; }
        [JsonIgnore]
        public ZohoSubscriptionHistory ZohoSubscriptionHistory { get; set; }
    
    }

}
