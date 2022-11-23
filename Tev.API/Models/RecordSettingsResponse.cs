using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class RecordSettingsResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public recordSettingsData data { get; set; }
        
    }

    public class recordSettingsData
    {
        public bool Connected { get; set; }
        public Event Event { get;set; }
        public bool Offline { get; set; }
        public bool EventRec { get; set; }
        public bool Schedule { get; set; }
        public string Manual { get; set; }
        public string Resolution { get; set; }
        public bool Recording_in_Progress { get; set; }
        public bool Fifo { get; set; }
        public List<Schedule> recordSchedules { get; set; }
    }
    public class Event
    {
        public bool eventRecording { get; set; }
        public EventType eventType { get; set; }
    }
    public class ResponseDTO
    {
        public bool Status { get; set; }
        public string Message { get; set; }

    }

}
