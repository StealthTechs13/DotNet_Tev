using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tev.Cosmos.Entity
{
    public class DeviceSetup
    {
        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("logicalDeviceId")]
        public string LogicalDeviceId { get; set; }

        [JsonProperty("orgId")]
        public int OrgId { get; set; }

        [JsonProperty("messageCode")]
        public int MessageCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("exception")]
        public string Exception { get; set; }

        [JsonProperty("retrying")]
        public bool? Retrying { get; set; }
        [JsonProperty("retryCount")]
        public int? RetryCount { get; set; }
        public Status Status { get; set; }
    }

    public enum Status
    {
        InProgress,
        Complete,
        Error
    }
}
