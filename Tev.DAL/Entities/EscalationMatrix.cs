using MMSConstants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    //Attention to Time is alwayes in minute

    public class EscalationMatrix : Entity
    {
        [Key]
        public string EscalationMatrixId { get; set; }
        public int OrganizationId { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverDescription { get; set; }
        public string ReceiverPhone { get; set; }
        public EscalationLevelEnum EscalationLevel { get; set; }
        public SmokeValueEnum SmokeValue { get; set; }
        public string SenderPhone { get; set; }
        public SmokeStatusEnum SmokeStatus { get; set; }
        public decimal AttentionTime { get; set; }
        public string DeviceId { get; set; }
    }
}
