using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class InvoiceHistoryResponse
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public string InvoiceNumber { get; set; }
        public double Balance { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Email { get; set; }
        public string CustomerName { get; set; }
        public string InvoiceId { get; set; }
        public double Total { get; set; }

        public ICollection<InvoiceHistoryItemResponse> InvoiceItems { get; set; }
        public ICollection<InvoiceHistoryPaymentResponse> Payments { get; set; }
        public ICollection<InvoiceHistorySubscriptionResponse> Subscriptions { get; set; }
    }

    public class InvoiceHistoryItemResponse
    {
        public string ItemCode { get; set; }
        public int Quantity { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public int InvoiceHistoryId { get; set; }
    }
    public class InvoiceHistorySubscriptionResponse
    {
        public string SubscriptionId { get; set; }
        public int InvoiceHistoryId { get; set; }
        public DateTime ActivatedTime { get; set; }
        public DateTime PurchaseTime { get; set; }
    }
    public class InvoiceHistoryPaymentResponse
    {
        public string PaymentId { get; set; }
        public double Amount { get; set; }
        public double AmountRefunded { get; set; }
        public double BankCharges { get; set; }
        public string Description { get; set; }
        public int InvoiceHistoryId { get; set; }
    }
}
