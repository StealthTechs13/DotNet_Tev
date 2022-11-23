using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class ZohoSubscriptionWebHookRequestModel
    {
        public string created_time { get; set; }
        public string event_id { get; set; }
        public string event_type { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public Subscription subscription { get; set; }
    }
    public class CustomField
    {
        public string customfield_id { get; set; }
        public string label { get; set; }
        public string value { get; set; }
    }
    public class Subscription
    {
        public string subscription_number { get; set; }
        public string subscription_id { get; set; }
        public List<SubscriptionAddOn> addons { get; set; }
        public Plan plan { get; set; }
        public List<CustomField> custom_fields { get; set; }
        public List<SubscrptionTax> taxes { get; set; }
        public string product_name { get; set; }
        public string created_time { get; set; }
        public string status { get; set; }
        public string activated_at { get; set; }
        public int interval { get; set; }
        public string interval_unit { get; set; }
        public string current_term_starts_at { get; set; }
        public string current_term_ends_at { get; set; }
        public int tax_percentage { get; set; }
        public double sub_total { get; set; }
        public double amount { get; set; }
        public string currency_code { get; set; }
        public string child_invoice_id { get; set; }
        public CompanyDetails customer { get; set; }
        public string next_billing_at { get; set; }

    }
    public class SubscriptionAddOn
    {
        public string name { get; set; }
        public double price { get; set; }
        public string addon_code { get; set; }
    }
    public class Plan
    {
        public string name { get; set; }
        public string plan_code { get; set; }
        public int quantity { get; set; }
        public double price { get; set; }
        public int tax_percentage { get; set; }
    }
    public class SubscrptionTax
    {
        public string tax_name { get; set; }
        public double tax_amount { get; set; }
    }
    public class CompanyDetails
    {
        public string company_name { get; set; }
        public string email { get; set; }
    }

}


