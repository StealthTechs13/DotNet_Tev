using System;
using System.Collections.Generic;
using System.Text;

namespace Tev.Cosmos.Entity
{
   public class WSDSummaryEntity
    {
        public int SmokeValue { get; set; }
        public double? BatteryValue { get; set; }
        public int? SmokeStatus { get; set; }
        public int? BatteryStatus { get; set; }
        public long? EnqueuedTimestamp { get; set; }

        public int? AlertType { get; set; }

        public int? TestId { get; set; }
    }

    public class WSDSummaryDataEntity : WSDSummaryEntity
    {
        public string MspVersionTargeted { get; set; }
        public string CcVersionTargeted { get; set; }
        public string MspVersionDevice { get; set; }
        public string CcVersionDevice { get; set; }
        public string CertificateId { get; set; }

        public string LogicalDeviceId { get; set; }

        public string DeviceSerialNumber { get; set; }

    }
}
