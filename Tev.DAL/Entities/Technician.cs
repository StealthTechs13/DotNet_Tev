using MMSConstants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class Technician : Entity
    {
        [Key]
        public string TechnicianId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public TechnicianTypeEnum TechnicianType { get; set; }
        public string Address { get; set; }
        public List<TechnicianDevices> TechnicianDevices { get; set; }
        public Technician()
        {
            this.TechnicianDevices = new List<TechnicianDevices>();
        }
    }
}
