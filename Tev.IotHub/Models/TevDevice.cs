using MMSConstants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tev.IotHub.Models
{
    public class TevDevice
    {
        public string OrgId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public string LocationId { get; set; }
        public string LocationName { get; set; }
        public string ConnectionState { get; set;}
        public string MacAddress { get; set; }
        public string FirmwareVersion { get; set; }
        public string WifiName { get; set; }
        public string SubscriptionId { get; set; }
        public string SubscriptionExpiryDate { get; set; }
        public Applications DeviceType { get; set; }
        public List<int> AvailableFeatures { get; set; }
        public string SubscriptionStatus { get; set; }
        public bool? Deleted { get; set; }
        public bool? Disabled { get; set; }
        public string NewFirmwareVersion { get; set; }
        public DateTime LastActivityTime { get; set; }
    }
    public class TevDeviceExtension : TevDevice
    {
        public string ActualDeviceId { get; set; }
        public bool? IsLiveStreaming { get; set; }
        public bool? UserApproved { get; set; }
    }
}
