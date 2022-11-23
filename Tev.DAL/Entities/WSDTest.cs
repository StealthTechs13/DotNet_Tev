using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Tev.DAL.Entities
{
    public class WSDTest:Entity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public int? GTemperatureSensorOffset2 { get; set; }
        
        public int? GTemperatureSensorOffset { get; set; }
        
        public int? ClearAir { get; set; }
        
        public int? IREDCalibration { get; set; }
        
        public int? PhotoOffset { get; set; }
       
        public int? DriftLimit { get; set; }
        
        public int? DriftBypass { get; set; }
       
        public int? TransmitResolution { get; set; }
        
        public int? TransmitThreshold { get; set; }

        public int? SmokeThreshold { get; set; }
       
        public int? SmokeValue { get; set; }
     
    }
}
