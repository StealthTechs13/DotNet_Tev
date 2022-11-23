using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class LiveStreaming:Entity
    {
        [Key]
        public string LogicalDeviceId { get; set; }
        public DateTime StartedUTC { get; set; }
        public int SecondsLiveStreamed { get; set; }
        public string Status { get; set; }
    }
}
