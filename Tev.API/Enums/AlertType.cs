using MMSConstants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Enums
{
    public enum AlertType
    {
        [EnumAttribute("Crowd", "Crowd")]
        Crowd =1,
        [EnumAttribute("Last7daysCloudStorageForAlerts", "Last7daysCloudStorageForAlerts")] 
        Last7daysCloudStorageForAlerts = 2,
        [EnumAttribute("Loiter", "Loiter")]
        Loiter =3,
        [EnumAttribute("Mask", "Mask")]
        Mask =4,
        [EnumAttribute("Helmet", "Helmet")]
        Helmet =5,
        [EnumAttribute("NoMask","No Mask")]
        NoMask=6,
        [EnumAttribute("Trespassing", "Trespassing")]
        Trespassing =7,
        //Smart AI Supervision Offline Online Alert
        [EnumAttribute("SmartAICameraOffline", "Smart AI Supervision Offline")]
        SmartAICameraOffline = 50,
        [EnumAttribute("SmartAICameraOnline", "Smart AI Supervision Online")]
        SmartAICameraOnline = 51,
        // This is a WSD alert type , it has nothing to do with TEV 
        [EnumAttribute("Fire", "Smoke Detected")]
        Fire =100,
        [EnumAttribute("FireTest", "Test Smoke Detected")]
        FireTest =101,
        [EnumAttribute("FireOffline", "Smoke Detector Offline")]
        FireOffline =102,
        [EnumAttribute("FireOnline","Smoke Detector Online")]
        FireOnline=103,
        [EnumAttribute("FireOfflineIncident", "Offline Incident Alert")]
        FireOfflineIncident = 104,
        [EnumAttribute("DeviceReplacedAlert", "Device name replaced")]
        DeviceReplacedAlert = 105
    }

    public enum FeatureType
    {
        [EnumAttribute("MobileNotifications", "Mobile Notifications")]
        Mobile_Notifications = 1,
        [EnumAttribute("CallonMobile", "Call on Mobile")]
        Call_on_Mobile = 2
    }
}
