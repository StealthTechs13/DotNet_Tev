using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class ZohoPurchaseWebhookRequestModel
    {
        public string created_time { get; set; }
        public string event_type { get; set; }
        public PaymentDataWebhookRequestModel data { get; set; }
    }

    public class PaymentDataWebhookRequestModel
    {
        public PayementWebhookRequestModel payment { get; set; }
    }

    public class PayementWebhookRequestModel
    {
        public string date { get; set; }
        public string description { get; set; }
        public string currency_code { get; set; }
        public double bank_charges { get; set; }
        public string payment_number { get; set; }
        public string payment_id { get; set; }
        public string email { get; set; }
        public double amount { get; set; }
        public string customer_id { get; set; }
        public List<InvoiceWebhookRequestModel> invoices { get; set; }
        public string status { get; set; }
    }

    public class InvoiceWebhookRequestModel
    {
        public string date { get; set; }
        public double balance_amount { get; set; }
        public string invoice_id { get; set; }
        public string invoice_number { get; set; }
        public double invoice_amount { get; set; }
        public double amount_applied { get; set; }
        public string transaction_type { get; set; }
        public string[] subscription_ids { get; set; }
    }
}
