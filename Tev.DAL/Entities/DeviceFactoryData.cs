using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
   public class DeviceFactoryData : Entity
    {
        [Key]
        public int Id { get; set; }
        public string DeviceName { get; set; }

        public bool Result { get; set; }

        public string CertificateID { get; set; }

        public string MspTargetedversion { get; set; }

        public string CcTargetedversion { get; set; }

        public string MspDeviceversion { get; set; }

        public string CcDeviceversion { get; set; }

        public string LogicalDeviceID { get; set; }

        public string FailureReasons { get; set; }

        public int SmokeValue { get; set; }
    }
}
