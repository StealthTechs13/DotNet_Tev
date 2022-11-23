using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.API.Models
{
    public class GetZoneFencingResponse
    {
        /// <summary>
        /// Yes if zone fencing is required, no if not required
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// (x1,y1) and (x2,y2) corordinates
        /// </summary>
        public Zone Zone { get; set; }

        public string ImageUrl { get; set; }
    }
}
