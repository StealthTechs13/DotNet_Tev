using MMSConstants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class TechnicianDevices : Entity
    {
        [Key]
        public string TechnicianDeviceId { get; set; }
        public string TechnicianId { get; set; }
        public Technician Technician { get; set; }
        public Applications DeviceType { get; set; }
    }
}
