using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class VideoStreamingStartResponse
    {
        /// <summary>
        /// URL for subscription to signalR notifications
        /// </summary>
        public string SignalRNegotiateUrl { get; set; }

        /// <summary>
        /// Stop sequence number to be sent to STOP video API when the user clicks the stop video button
        /// </summary>
        public long StopSequenceNumber { get; set; }

        public bool IsLiveStreaming { get; set; } = false;

        /// <summary>
        /// The HLS URL for live streaming
        /// </summary>
        public string HLSUrl { get; set; }
        public int RetryAfter { get; set; } = 10;
    }
}
