using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class FileStorageDurationResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
    }

    public class RecordVedioResolutionResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public string Resolution { get; set; }
    }
    public class RecordVedioResolutionRequest
    {
        
        public string device_id { get; set; }
        public string Resolution { get; set; }
    }


}
