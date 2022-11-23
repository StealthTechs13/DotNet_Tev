using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Tev.DAL.Entities
{
    public class PaymentHistory:Entity
    {
        [Key]
        public int Id { get; set; }
        public DateTime PaymentCreatedTime { get; set; }
        public string EventType { get; set; }
        public string PaymentId { get; set; }
        public string PaymentNumber { get; set; }
        public double PayedAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Description { get; set; }
        public string CurrencyCode { get; set; }
        public string CustomerId { get; set; }
        public string Email { get; set; }
        public string PaymentStatus { get; set; }
        public ICollection<PayementInvoiceAssociation> PayementInvoiceAssociations { get; set; }

    }

    public class PayementInvoiceAssociation:Entity
    {
        [Key]
        public int Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string InvoiceId { get; set; }
        public string TransactionType { get; set; }
        public string InvoiceNumber { get; set; }
        public double InvoiceAmount { get; set; }
        public double AmountApplied { get; set; }
        public double BalanceAmount { get; set; }
        public int PaymentHistoryFK { get; set; }
        [JsonIgnore]
        public PaymentHistory PaymentHistory { get; set; }
        [JsonIgnore]
        public ICollection<InvoiceSubscriptionAssociation> InvoiceSubscriptionAssociations { get; set; }
    }

    public class InvoiceSubscriptionAssociation:Entity
    {
        [Key]
        public int Id { get; set; }
        public string InvoiceId { get; set; }
        public string SubscriptionId { get; set; }
        [JsonIgnore]
        public int PayementInvoiceAssociationFK { get; set; }
        [JsonIgnore]
        public PayementInvoiceAssociation PayementInvoiceAssociation { get; set; }
    
    }
}
