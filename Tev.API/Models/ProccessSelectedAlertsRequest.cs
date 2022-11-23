using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class ProccessSelectedAlertsRequest
    {
        public List<string> AlertIds { get; set; }

        public string Action { get; set; }
    }
}
