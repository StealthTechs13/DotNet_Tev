using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tev.IotHub.Models;

namespace Tev.API.Models
{
    public class SetResetZoneFencingRequest
    {
        /// <summary>
        /// Yes if zone fencing is required, no if not required
        /// </summary>
        [Required]
        public bool Enabled { get; set; }

        /// <summary>
        /// (x1,y1) and (x2,y2) corordinates
        /// </summary>
        public Zone Zone { get; set; }

        /// <summary>
        /// The width of the image on client
        /// </summary>
        public int ClientImageWidth { get; set; }

        /// <summary>
        /// Height of image on client
        /// </summary>
        public int ClientImageHeight { get; set; }
    }
}
