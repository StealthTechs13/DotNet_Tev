using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class ZohoSubscriptionRequest
    {
        /// <summary>
        /// Subscription id
        /// </summary>
        public string SubscriptionId { get; set; }
        /// <summary>
        /// Plan code
        /// </summary>
        public string PlanCode { get; set; }
        /// <summary>
        /// Logical device id
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// AddOn / Feature(s) id's in comma seperated 
        /// </summary>
        public string [] Addons { get; set; }
        /// <summary>
        /// headless true / false value tells whether the request is from web or mobile , 
        /// If headless value is true that means request coming from mobile 
        /// else the request came from web app.
        /// </summary>
        public bool Headless { get; set; }
    }

    public class CreateNewSubscriptionRequest
    {
        /// <summary>
        /// Plan code 
        /// </summary>
        public string PlanCode { get; set; }
        /// <summary>
        /// Logical device id
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// AddOn / Feature(s) id's in comma seperated 
        /// </summary>
        public string[] Addons { get; set; }
        /// <summary>
        /// Headless true / false value tells whether the request is from web or mobile , 
        /// If headless value is true that means request coming from mobile 
        /// else the request came from web app.
        /// </summary>
        public bool Headless { get; set; }
    }

    public class ComputeCostRequest
    {
        /// <summary>
        /// Subscription Id
        /// </summary>
        public string SubscriptionId { get; set; }
        /// <summary>
        /// Plan code 
        /// </summary>
        public string PlanCode { get; set; }
        /// <summary>
        /// AddOn / Feature(s) id's in comma seperated 
        /// </summary>
        public string[] Addons { get; set; }
    }

    public class CreateNewSubscriptionRequestExtension : CreateNewSubscriptionRequest
    {
        public string SecretKey { get; set; }
    }
}
