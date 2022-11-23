using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Tev.DAL.Entities
{
    public class InvoiceHistory: Entity
    {
        [Key]
        public int Id { get; set; }
        public string OrgId { get; set; }
        public string EventType { get; set; }
        public DateTime CreatedTime { get; set; }
        public string InvoiceNumber { get; set; }
        public double Balance { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Email { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string InvoiceId { get; set; }
        public double Total { get; set; }
        public ICollection<InvoiceHistoryItem> InvoiceItems { get; set; }
        public ICollection<InvoiceHistoryPayment> Payments { get; set; }
        public ICollection<InvoiceHistorySubscription> InvoiceHistorySubscriptionAssociations { get; set; }
    }
    public class InvoiceHistoryItem:Entity
    {
        [Key]
        public int Id { get; set; }
        public string ItemCode { get; set; }
        public int Quantity { get; set; }
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        [JsonIgnore]
        public int InvoiceHistoryFK { get; set; }
        [JsonIgnore]
        public InvoiceHistory InvoiceHistory { get; set; }
    }
    public class InvoiceHistorySubscription:Entity
    {
        [Key]
        public int Id { get; set; }
        public string SubscriptionId { get; set; }
        [JsonIgnore]
        public int InvoiceHistoryFK { get; set; }
        [JsonIgnore]
        public InvoiceHistory InvoiceHistory { get; set; }
    }
    public class InvoiceHistoryPayment:Entity
    {
        [Key]
        public int Id { get; set; }
        public string PaymentId { get; set; }
        public double Amount { get; set; }
        public double AmountRefunded { get; set; }
        public double BankCharges { get; set; }
        public string Description { get; set; }
        [JsonIgnore]
        public int InvoiceHistoryFK { get; set; }
        [JsonIgnore]
        public InvoiceHistory InvoiceHistory { get; set; }
    }
}

