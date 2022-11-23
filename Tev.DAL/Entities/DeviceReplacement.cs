using MMSConstants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class DeviceReplacement : Entity
    {
        [Key]
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public string OrgId { get; set; }
        public string Email { get; set; }
        public string Comments { get; set; }
        public ReplaceStatusEnum ReplaceStatus { get; set; }
    }
}
