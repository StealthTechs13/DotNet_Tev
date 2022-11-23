using MMSConstants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class EscalationMatrixModel
    {
        [JsonProperty("escalationMatrixId")]
        public string EscalationMatrixId { get; set; }
        [Required(ErrorMessage = "Organization Id is required.")]
        [JsonProperty("organizationId")]
        public int OrganizationId { get; set; }
        [Required(ErrorMessage = "Receiver Name is required")]
        [JsonProperty("receiverName")]
        public string ReceiverName { get; set; }
        [JsonProperty("receiverDescription")]
        public string ReceiverDescription { get; set; }
        [Required(ErrorMessage = "Receiver Phone is required")]
        [JsonProperty("receiverPhone")]
        public string ReceiverPhone { get; set; }
        [JsonProperty("escalationLevel")]
        public string EscalationLevel { get; set; }
        [Required(ErrorMessage = "Smoke Value is required")]
        [JsonProperty("smokeValue")]
        public string SmokeValue { get; set; }
        [Required(ErrorMessage = "Sender Phone is required")]
        [JsonProperty("senderPhone")]
        public string SenderPhone { get; set; }
        [Required(ErrorMessage = "Smoke Status is required")]
        [JsonProperty("smokeStatus")]
        public string SmokeStatus { get; set; }
        [JsonProperty("attentionTime")]
        public decimal AttentionTime { get; set; }
        [Required(ErrorMessage = "Device Id is required")]
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }
    }

    public class GetAllEscalationPaginateReq : BasePaginatedReq
    {
        [JsonProperty("organizationId")]
        public int? OrganizationId { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }
        
        public GetAllEscalationPaginateReq() : base()
        {

        }
    }

    public class GetAllDisplayResponse 
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
