using MMSConstants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tev.IotHub.Models
{
    public class DeviceTwinProperty
    {
        public Property properties { get; set; }
        public Tag tags { get; set; }
    }
    public class Tag
    {
        public string subscriptionId { get; set; }
        public string status { get; set; }
        public TwinChangeStatus twinChangeStatus { get; set; }
    }
    public class Property
    {
        public Desired desired { get; set; }
    }
    public class Desired
    {
        public Subscription subscription { get; set; }
        public FeatureConfig featureConfig { get; set; }
        public string locationId { get; set; }
        public string locationName { get; set; }
        public string deviceName { get; set; }
        public bool deleted { get; set; }
        public bool disabled { get; set; }
        public bool factoryReset { get; set; } = false;
        public LiveStreaming live_streaming { get; set; }
        public Firmware firmware { get; set; }
        public SDCard sdCard { get; set; }
        public EventRecording eventRecording { get; set; }
    }
    public class Subscription
    {
        public bool isActive { get; set; }
        public string expiry { get; set; }
        public int[] features { get; set; }
    }

    public class Feature
    {
        public FeatureConfig featureConfig { get; set; }
        public Feature()
        {
            this.featureConfig = new FeatureConfig();
        }
    }
    public class FeatureConfig
    {
        public bool? uploadPic { get; set; }
        public bool? zoneFencingEnabled { get; set; }
        public Crowd crowd { get; set; }
        public Trespassing trespassing { get; set; }
        public Loiter loiter { get; set; }
        public Zone zone { get; set; }
        public bool? buzzerControl { get; set; } = false;
        public float score_val { get; set; } = 0.5f;

        public VideoResolution videoResolution { get; set; }

    }
    public class SDCard
    {
        public string playbackVideo { get; set; }
        public int recordingType { get; set; }
        public int fifo { get; set; }
        public List<Schedule> recordingSchedule { get; set; }
        public string resolution { get; set; }
        public EventRecording eventRecording { get; set; }

    }
    public class EventRecording
    {
        public List<int> eventList { get; set; }
    }


    public class Schedule
    {
        public bool enabled { get; set; }
        public bool fullday { get; set; }
        public List<time> times { get; set; }
    }

    
    
    public class time
    {
        public string st { get; set; }
        public string et { get; set; }
    }
    public class LiveStreaming
    {
        public bool stream { get; set; }
        public string secret_key { get; set; }
        public string stream_name { get; set; }
        public string access_key { get; set; }
        public string aws_region { get; set; }
        public long servicebus_sequence_number { get; set; }
        public long auto_stop_servicebus_seq_num { get; set; }
    }
    
        
   

    public class VideoResolution
    {
        public string hls { get; set; }
        public string webRtc { get; set; }

        public string sdCard { get; set; }
    }
    public class Firmware
    {
        public bool userApproved { get; set; } = false;

    }

    public class Zone
    {
        public int x1 { get; set; }
        public int y1 { get; set; }
        public int x2 { get; set; }
        public int y2 { get; set; }
    }

    public class Crowd
    {
        public int crowdLimit { get; set; }

        public List<RecordSchedule> CrowdSchedule { get; set; }
    }
    public class Trespassing
    {
        /// <summary>
        /// Time in 24 hours format in IST e.g. "20:30" for TEV1 and TEV 2 with firmware less than 2.2
        /// </summary>
        public string trespassingStartTime { get; set; }
        /// <summary>
        /// Time in 24 hours format in IST e.g. "60:30" for TEV1 and TEV 2 with firmware less than 2.2
        /// </summary>
        public string trespassingEndTime { get; set; }

        public List<RecordSchedule> TrespassingSchedule { get; set; }

    }
    public class Loiter
    {
        public int time { get; set; }

        public List<RecordSchedule> LoiterSchedule { get; set; }
    }
}

