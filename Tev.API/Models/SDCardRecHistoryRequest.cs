using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class SDCardRecHistoryRequest
    {
        public string deviceId { get; set; }
        public string secretKey { get; set; } 
        public List<SDCardRecHistory> sdCardRecHistory { get; set; }
        public List<String> eventRecHistory { get; set; }
    }
    public class SDCardRecHistory
    {
        public string date { get; set; }
        public List<RecDateTime> sdRecTime { get; set; }
    }
    public class RecDateTime
    {
        public string st { get; set; }
        public string et { get; set; }
        public string s { get; set; }
    }
}
