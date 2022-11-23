using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class SrtSessionDetail:Entity
    {
        [Key]
        public string Id { get; set; }
        public string sessionID { get; set; }
        public string displayName { get; set; }
        public long startAt { get; set; }
        public long expireAt { get; set; }
        public bool isLicensed { get; set; }
    }
}
