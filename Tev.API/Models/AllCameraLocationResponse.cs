using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class AllCameraLocationResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<AllCamareaLocationData> data { get; set; }
    }
    public class AllCamareaLocationData
    {
        public string Location { get; set; }
        public List<string> cameras { get; set; }
    }
}
