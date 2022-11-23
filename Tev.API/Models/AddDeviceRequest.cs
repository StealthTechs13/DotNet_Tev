using MMSConstants;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class AddDeviceRequest
    {
        /// <summary>
        /// Logical device id
        /// </summary>
        [Required]
        public Guid DeviceId { get; set; }

        /// <summary>
        /// If the site is new for exisiting
        /// </summary>
        [DefaultValue(false)]
        public bool IsNewSite { get; set; }

        /// <summary>
        /// Not required when IsNewSite is false
        /// </summary>
        public string SiteName { get; set; }

        /// <summary>
        /// Site id to which device will be attached
        /// </summary>
        [Required]
        public Guid SiteId { get; set; }

        /// <summary>
        /// Application name
        /// </summary>
        [Required]
        public Applications Application { get; set; }

        public string DeviceName { get; set; }

        public string OrgId { get; set; }

        public string DeviceType { get; set; }
    }
}
