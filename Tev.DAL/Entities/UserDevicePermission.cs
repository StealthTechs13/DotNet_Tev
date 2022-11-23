using MMSConstants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class UserDevicePermission : Entity
    {
        [Key]
        public string UserDevicePermissionId { get; set; }
        public string UserEmail { get; set; }
        public string DeviceId { get; set; }
        public Applications DeviceType { get; set; }
        public DevicePermissionEnum DevicePermission { get; set; }
    }
}
