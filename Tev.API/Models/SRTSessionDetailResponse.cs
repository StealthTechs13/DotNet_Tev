using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class SRTSessionDetailResponse
    {
        public string sessionID { get; set; }
        public string displayName { get; set; }
        public string email { get; set; }
        public List<string> roles { get; set; }
        public long startAt { get; set; }
        public long expireAt { get; set; }
        public object lastLoginDate { get; set; }
        public object numLoginFailures { get; set; }
        public bool isLicensed { get; set; }
    }
}
