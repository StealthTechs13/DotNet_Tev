using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class QRAuthCode:Entity
    {
        [Key]
        public int Id { get; set; }
        public string LogicalDeviceId { get; set; }
        public int Code { get; set; }
        public string Type { get; set; }
    }
}
