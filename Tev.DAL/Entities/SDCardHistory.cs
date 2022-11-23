using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class SDCardHistory : Entity
    {
        [Key]
        public string Id { get; set; }
        public string deviceId { get; set; }
        public int type { get; set; }//0=other,1=event
        public string date { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public string size { get; set; }
    }
}
