using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class DeviceStreamingTypeManagement : Entity
    {
        /// <summary>
        /// Primary Key
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// User Name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Camera device logical id
        /// </summary>
        public string LogicalDeviceId { get; set; }

        /// <summary>
        /// User authentication JWT token
        /// </summary>
        public string UserTokenId { get; set; }

        /// <summary>
        /// Live streaming active. True/False 
        /// </summary>
        public bool LiveStreamingActive { get; set; }

        /// <summary>
        /// Play back streaming active. True/False
        /// </summary>
        public bool PlaybackStreamingActive { get; set; }
        
        /// <summary>
        /// Is any user active and watching streaming 
        /// </summary>
        public bool IsUserStreaming { get; set; }

        /// <summary>
        /// Org Id
        /// </summary>
        public string OrgId { get; set; }
    }
}
