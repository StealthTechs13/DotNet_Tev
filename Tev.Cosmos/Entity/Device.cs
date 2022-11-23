using MMSConstants;
using System;
using System.Collections.Generic;
using System.Text;


namespace Tev.Cosmos.Entity
{
    public class Device
    {
        public string Id { get; set; }
        public string OrgId { get; set; }
        public string LogicalDeviceId { get; set; }
        public string DeviceType { get; set; }
        public string DeviceName { get; set; }
        public string LocationId { get; set; }
        public string LocationName { get; set; }
        public string CurrentFirmwareVersion { get; set; }
        public string WifiName { get; set; }
        public long CreatedOn { get; set; }

        //default data read from config
        public int HealthPacketFrequency { get; set; }
        public FeatureConfig FeatureConfig { get; set; }
        public bool Online { get; set; }
        public Subscription Subscription { get; set; }
        public Firmware Firmware { get; set; }
        public LiveStreaming LiveStreaming { get; set; }
        public string FactoryResetAuthCode { get; set; }
        public TwinChangeStatus TwinChangeStatus { get; set; } = TwinChangeStatus.Default;
        public string BatteryStatus { get; set; }
        public long? BatteryStatusDate { get; set; }

        public string mspVersion { get; set; }

        public bool sdCardStatus { get; set; }
        public string sdCardPassPhrase { get; set; }
        public bool SdCardAvilable { get; set; }
        public string SdCardTotalSpace { get; set; }
        public bool SdCardCorrupted { get; set; }
        public string SdCardRecordingStatus { get; set; }
        public string SdCardSpaceLeft { get; set; }
        public string SDCardTimeStamp { get; set; }
        public bool isSDcardfull { get; set; }
        public string SecretKey { get; set; }
        public List<RecHistory> recHistories { get; set; }
        public RecordSetting recordSetting { get; set; }
        public List<RecordSchedules> recordSchedules { get; set; }
        public bool isSDCardfull { get; set; }
        public List<VideoList> videoLists { get; set; }
        public bool Encryption { get; set; }
    }
    public class VideoList
    {
        public string filename { get; set; }
        public string size { get; set; }
        public string duration { get; set; }
        public bool Isuploaded { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime timestamp { get; set; }
    }
    public class RecHistory
    {
        public int recType { get; set; } //1 for schdeule,2 for event,3 for manual,4 offine
        public string date { get; set; }
        public string starttime { get; set; }
        public string endtime { get; set; }
        public string edate { get; set; }
    }
    public class RecordSchedules
    {
        public bool fullday { get; set; }
        public string day { get; set; }
        public List<Scheduletime> time { get; set; }
    }
    public class Scheduletime
    {
        public string starttime { get; set; }
        public string endtime { get; set; }
    }

    public class FeatureConfig
    {
        public bool ZoneFencingEnabled { get; set; } = false;
        public bool BuzzerControl { get; set; } = false;
        public Crowd Crowd { get; set; }
        public Trespassing Trespassing { get; set; }
        public Loiter Loiter { get; set; }
        public Zone Zone { get; set; }
        public float PersonDetectionSensitivity { get; set; } = 0.5f;

        public VideoResolution VideoResolution { get; set; }
    }

    public class LiveStreaming
    {
        public bool IsLiveStreaming { get; set; } = false;
    }

    public class VideoResolution
    {
        public string hls { get; set; }
        public string webRtc { get; set; }

        public string sdCard { get; set; }
        public string livestream { get; set; }

    }

    public class Firmware
    {
        public string NewFirmwareVersion { get; set; }
        public bool UserApproved { get; set; } = false;
    }

    public class Crowd
    {
        public int CrowdLimit { get; set; }
        public bool SchedulingSupported { get; set; }
        public List<RecordSchedule> CrowdSchedule { get; set; }
    }
    public class Trespassing
    {
        /// <summary>
        /// Time in 24 hours format in IST e.g. "20:30"
        /// </summary>
        public string TrespassingStartTime { get; set; }
        /// <summary>
        /// Time in 24 hours format in IST e.g. "60:30"
        /// </summary>
        public string TrespassingEndTime { get; set; }
        public bool SchedulingSupported { get; set; }
        public List<RecordSchedule> TrespassingSchedule { get; set; }
    }

    public class Loiter
    {
        public int Time { get; set; }
        public bool SchedulingSupported { get; set; }
        public List<RecordSchedule> LoiterSchedule { get; set; }
    }
    public class Zone
    {
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
    }

    public class Subscription
    {
        public string SubscriptionId { get; set; }
        public string PlanName { get; set; }
        public string Amount { get; set; }
        public string SubscriptionExpiryDate { get; set; }
        public int[] AvailableFeatures { get; set; }
        public string SubscriptionStatus { get; set; }
    }
    public class RecordSetting
    {
        public bool scheduleRecording { get; set; }
        public bool eventRecording { get; set; }
        public bool offlineRecording { get; set; }
        public string manualRecording { get; set; }
        public bool recodringProgrees { get; set; }
        public bool loiter { get; set; }
        public bool crowd { get; set; }
        public bool trespassing { get; set; }
        public bool fifo { get; set; }
    }
}
