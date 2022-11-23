using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class ZohoInvoiceWebhookRequestModel
    {
        public string created_time { get; set; }
        public string event_type { get; set; }
        public InvoiceHistoryData data { get; set; }
    }
    public class InvoiceHistoryData
    {
        public InvoiceReqModel invoice { get; set; }
    }
    public class InvoiceReqModel
    {
        public string updated_time { get; set; }
        public string number { get; set; }
        public double balance { get; set; }
        public string currency_code { get; set; }
        public string invoice_id { get; set; }
        public string email { get; set; }
        public string customer_id { get; set; }
        public string customer_name { get; set; }
        public double total { get; set; }
        public List<InvoiceItem> invoice_items { get; set; }
        public List<InvoiceHistorySusbcriptionReqModel> subscriptions { get; set; }
        public List<InvoiceHistoryPaymentReqmodel> payments { get; set; }
        public List<CustomField> custom_fields { get; set; }
    }
    public class InvoiceItem
    {
        public string code { get; set; }
        public int quantity { get; set; }
        public string item_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public double price { get; set; }
    }
    public class InvoiceHistorySusbcriptionReqModel
    {
        public string subscription_id { get; set; }
    }
    public class InvoiceHistoryPaymentReqmodel
    {
        public string payment_id { get; set; }
        public double amount { get; set; }
        public double amount_refunded { get; set; }
        public double bank_charges { get; set; }
        public string description { get; set; }
    }
}

