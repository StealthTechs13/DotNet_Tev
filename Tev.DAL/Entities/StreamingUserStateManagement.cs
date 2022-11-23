using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class StreamingUserStateManagement : Entity
    {

        [Key]
        public int Id { get; set; }

        public string UserName { get; set; }

        public string LogicalDeviceId { get; set; }

        public string UserTokenId { get; set; }

        public DateTime LastUpdatedDate { get; set; }

        public bool LiveStreamingState { get; set; }

        public bool PlaybackStreamingState { get; set; }

        public bool IsUserActive { get; set; }

        public string OrgId { get; set; }
       
    }
}
