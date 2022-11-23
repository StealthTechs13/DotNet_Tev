using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class DownloadUrlsResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<DownloadUrlsObj> downloadUrlsObj { get; set; }
    }
    public class DownloadUrlsObj
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DownUrls { get; set; }
        public string filename { get; set; }
    }

  
}
