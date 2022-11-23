using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class StreamResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<StreamData> StreamData { get; set; }
        public int ErrorCode { get; set; }

        public Boolean PrimaryUserLiveStreamingState { get; set; }

        public Boolean PrimaryUserPlayBackStreamingState { get; set; }

        public string PrimaryUserDeviceId { get; set; }

        public string PrimaryUserTokenId { get; set; }

        public string StreamChangeAlertMessage { get; set; }

        public bool StreamChangeStatus { get; set; }

        public bool IsPrimaryUserPresent { get; set; }

    }
    public class StreamData
    {
        public string deviceId { get; set; }
        public string gateway_IP { get; set; }
        public int destinationPort { get; set; }
        public string passphrase { get; set; }
        public string state { get; set; }
        public string resolution { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        public string hubId { get; set; }
    }
    public class SrcStreamData
    {        
        public string gateway_IP { get; set; }
        public int sourcePort { get; set; }
        public string passphrase { get; set; }
        public string resolution { get; set; }
    }
}
