using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Enums;

namespace Tev.API.Models
{
    public class DeviceResponse
    {
        /// <summary>
        /// Device Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Device Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// True denotes online, false denotes offline, null denotes Indeterminate
        /// </summary>
        public bool Connected { get; set; }

        /// <summary>
        /// Location to which device is connected
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// Location to which device is connected
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// Physical address of the device
        /// </summary>
        public string MacAddress { get; set; }

        /// <summary>
        /// Current Firmware version of the device
        /// </summary>
        public string FirmwareVersion { get; set; }

        /// <summary>
        /// Wifi to which device is connected
        /// </summary>
        public string WifiName { get; set; }
        
        /// <summary>
        /// Zoho subsription id of the device
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// List of available features on the device
        /// </summary>
        public List<string> AvailableFeatures { get; set; }

        /// <summary>
        /// Date when current subscription expires
        /// </summary>
        public DateTime SubscriptionExpiryDate { get; set; }

        /// <summary>
        /// Device Permission For User
        /// </summary>
        public string CurrentUserPermission { get; set; }

        public List<DevicePermission> DevicePermissions { get; set; }

        public Applications DeviceType { get; set; }
        public string SubscriptionStatus { get; set; }
        public bool Disabled { get; set; } = false;
        public string NewFirmwareVersion { get; set; }

        public string mspVersion { get; set; }
        public bool isUpdateAvailable { get; set; }
        public long CreatedOn { get; set; }
        public string PlanName { get; set; }
        public string BatteryStatus { get; set; }
        public long? BatteryStatusDate { get; set; }

        public bool sdCardStatus { get; set; }

        public bool SdCardAvilable { get; set; } = false;

        public bool srtSupported { get; set; }

        public DeviceResponse()
        {
            DevicePermissions = new List<DevicePermission>();
        }
    }

    public class DevicePermission 
    {
        public string UserEmail { get; set; }
        public string Permission { get; set; }
    }
}
