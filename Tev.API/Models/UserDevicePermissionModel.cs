using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class UserDevicePermissionModel
    {
        
        [Required(ErrorMessage ="User Email is Required")]
        public string UserEmail { get; set; }
        public List<DevicePermissionModel> DevicePermission { get; set; }
        public UserDevicePermissionModel()
        {
            DevicePermission = new List<DevicePermissionModel>();
        }
    }

    public class DevicePermissionModel
    {
        public string UserDevicePermissionId { get; set; }

        [Required(ErrorMessage ="Device Id is required")]
        public string DeviceId { get; set; }
        [Required(ErrorMessage = "Permission is required")]
        public string Permission { get; set; }
    }

    public class UserDevicePermissionListModel
    {   
        public string UserEmail { get; set; }
        public List<DevicePermissionListModel> DevicePermission { get; set; }
        public UserDevicePermissionListModel()
        {
            DevicePermission = new List<DevicePermissionListModel>();
        }
    }

    public class DevicePermissionListModel
    {
        public string UserDevicePermissionId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public string Permission { get; set; }
    }

    public class GetUserDevicePermissionReq 
    {   
        public List<string> UserEmails { get; set; }

        public GetUserDevicePermissionReq()
        {
            UserEmails = new List<string>();
        }

    }
}
