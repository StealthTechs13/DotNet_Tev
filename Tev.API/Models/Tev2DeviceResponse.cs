using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class Tev2DeviceResponse
    {
        /// <summary>
        /// Device Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Device Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// True denotes online, false denotes offline, null denotes Indeterminate
        ///  /// </summary>
        public bool Connected { get; set; }

        /// <summary>
        /// Location to which device is connected
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// Location to which device is connected
        /// </summary>
        public string LocationName { get; set; }
        public Applications DeviceType { get; set; }
        public bool sdCardStatus { get; set; }
        public long CreatedOn { get; set; }
        public bool SdCardAvilable { get; set; } = false;
        public string CurrentUserPermission { get; set; }
        public List<DevicePermission> DevicePermissions { get; set; }
        public bool srtSupported { get; set; }
    }
}
