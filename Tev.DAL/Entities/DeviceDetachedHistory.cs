using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class DeviceDetachedHistory:Entity
    {
        [Key]
        public int Id { get; set; }
        public string PhysicalDetachedDeviceId { get; set; }
        public string LogicalDetachedDeviceId { get; set; }
        public string OrgId { get; set; }
        public string NewDeviceId { get; set; }
    }
}
