using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class CardResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public data data { get; set; }
    }
    public class data
    {
        public string record_status { get; set; }
        public string card_status { get; set; }
        public string total_space { get; set; }
        public string space_left { get; set; }
        public bool fifo { get; set; }
        public bool eventRecording { get; set; }
        public string resolution { get; set; }
        public int EnabledRecordingType { get; set; }
        public bool encryption { get; set; }
    }
}
