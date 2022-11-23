using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class EmergencyCallHistory : Entity
    {
        [Key]
        public string EmergencyCallHistoryId { get; set; }
        public string DeviceId { get; set; }
        public string Number { get; set; }
        public DateTime Time { get; set; }
        public string CallingPurpose { get; set; }

    }
}
