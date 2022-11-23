using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class AlertDetailResponse
    {
        public string AlertId { get; set; }

        public List<string> ImagePaths { get; set; }

        public string VideoUrl { get; set; }
    }
}
