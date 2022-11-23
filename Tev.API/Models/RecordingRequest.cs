using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class RecordingRequest
    {
        public string device_id { get; set; }
        public bool enable { get; set; }
        public string record { get; set; }
        public string settingType { get; set; }
        public List<Schedule> scheduleObj { get; set; }
        public EventType eventType { get; set; }
    }
    public class Schedule
    {
        public string day { get; set; }
        public bool fullday { get; set; }
        public List<time> time { get; set; }
    }
    public class time
    {
        public string starttime { get; set; }
        public string endtime { get; set; }
    }
    public class EventType
    {
        public bool Loiter { get; set; }
        public bool Crowd { get; set; }
        public bool Trespassing { get; set; }
    }

    public class Device_IdsList
    {
        public string getCameraId { get; set; }
        public List<SetCameraIds> setCameraIds { get; set; }
    }

    public class DeviceList
    {
        public List<SetCameraIds> setCameraIds { get; set; }
    }

    public class SetCameraIds
    {
        public string deviceId { get; set; }
    }
    public class DownloadVedioList
    {
        public List<CamerasList> camerasLists { get; set; }
    }
    public class CamerasList
    {
        public string deviceId { get; set; }
        public string filename { get; set; }
    }
    public class VideoList
    {
        public string deviceId { get; set; }
        public List<fileDetails> filedetails { get; set; }
    }
    public class fileDetails
    {
        public string filename { get; set; }
        public string size { get; set; }
        public string duration { get; set; }
        public string cameraName { get; set; }
        public string startDateTime { get; set; }
    }
}
