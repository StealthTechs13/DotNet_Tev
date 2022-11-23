using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Tev.Cosmos.Entity;

namespace Tev.Cosmos
{
    public class Alert:EntityBase
    {
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public int AlertType { get; set; }
        public string LocationId { get; set; }
        public string LocationName { get; set; }
        public bool Acknowledged { get; set; }
        public bool IsCorrect { get; set; }
        public string Comment { get; set; }
        public bool IsBookmarked { get; set; }
        public string Version { get; set; }
        public string OrgId { get; set; }
        public string TelemetryType { get; set; }
        public long OccurenceTimestamp { get; set; }
        public long EnqueuedTimestamp { get; set; }
        public long IngestionTimestamp { get; set; }
        public string DeviceType { get; set; }
        public int? SmokeValue { get; set; }
        public bool? IsDeleted { get; set; }

        public string AlertStatus { get; set; }
    }
}