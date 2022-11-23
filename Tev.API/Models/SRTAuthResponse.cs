using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class SRTAuthResponse
    {
        public Response response { get; set; }
    }
    public class Response
    {
        public string type { get; set; }
        public string message { get; set; }
        public string sessionID { get; set; }
        public long lastLoginDate { get; set; }
        public int numLoginFailures { get; set; }
    }
}
