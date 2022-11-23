using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class srtSessionResponse
    {
        public long collectedAt { get; set; }
        public Route route { get; set; }
    }

    public class ClientsStat
    {
        //public string label { get; set; }
        //public string srtVersion { get; set; }
        //public string SRTPeerVersion { get; set; }
        //public double bitrate { get; set; }
        //public int signalLosses { get; set; }
        //public double usedBandwidth { get; set; }
        //public string address { get; set; }
        //public int port { get; set; }
        public List<Connection> connections { get; set; }
    }

    public class Connection
    {
        public string state { get; set; }
        //public string address { get; set; }
        //public int port { get; set; }
        //public string localAddress { get; set; }
        //public int localPort { get; set; }
        //public string networkInterface { get; set; }
        //public double srtMaxBandwidth { get; set; }
        //public double srtCurrentBandwidth { get; set; }
        //public double srtEstimatedBandwidth { get; set; }
        //public int srtNumPackets { get; set; }
        //public int srtNumLostPackets { get; set; }
        //public int srtSkippedPackets { get; set; }
        //public int srtSkippedPacketsDiff { get; set; }
        //public double srtPacketLossRate { get; set; }
        //public int srtRetransmitRate { get; set; }
        //public int srtRoundTripTime { get; set; }
        //public int srtBufferLevel { get; set; }
        //public int srtNegotiatedLatency { get; set; }
        //public string srtEncryption { get; set; }
        //public string srtDecryptionState { get; set; }
        //public string srtFec { get; set; }
        //public int srtFecRows { get; set; }
        //public int srtFecCols { get; set; }
        //public string srtFecLayout { get; set; }
        //public string srtFecArq { get; set; }
        //public int srtFecRecoveredPackets { get; set; }
        //public int srtFecPacketLoss { get; set; }
        //public int srtFecTotalRecoveredPackets { get; set; }
        //public int srtFecTotalPacketLoss { get; set; }
        //public string srtGroupMemberStatus { get; set; }
        //public int srtGroupMemberWeight { get; set; }
        //public string srtGroupMode { get; set; }
        //public int numPackets { get; set; }
        //public int srtDroppedPackets { get; set; }
        //public int srtDroppedPacketsDiff { get; set; }
        //public string srtPeerDecryptionState { get; set; }
    }

    public class Destinations
    {
        //public string name { get; set; }
        //public string id { get; set; }
        //public string mode { get; set; }
        //public string protocol { get; set; }
        //public string state { get; set; }
        //public string elapsedRunningTime { get; set; }
        //public string srtGroupMode { get; set; }
        //public int srtConnectionLimit { get; set; }
        //public int srtRejectedCount { get; set; }
        public List<ClientsStat> clientsStat { get; set; }
        //public double bitrate { get; set; }
        //public int signalLosses { get; set; }
        //public double usedBandwidth { get; set; }
        //public double sendRate { get; set; }
        //public int numPackets { get; set; }
        //public string srtPeerDecryptionState { get; set; }
    }
   

    public class Route
    {
        //public string name { get; set; }
        //public string elapsedRunningTime { get; set; }
        //public string id { get; set; }
        //public string state { get; set; }
        //public Sources source { get; set; }
        public List<Destinations> destinations { get; set; }
    }

    public class Sources
    {
        public string name { get; set; }
        public string id { get; set; }
        public string mode { get; set; }
        public string protocol { get; set; }
        public string elapsedRunningTime { get; set; }
        public int signalLosses { get; set; }
        public double sendRate { get; set; }
        public int numPackets { get; set; }
        public double usedBandwidth { get; set; }
        public double bitrate { get; set; }
        public string state { get; set; }
        public int srtLatency { get; set; }
        public int srtRcvBuf { get; set; }
        public string srtGroupMode { get; set; }
        public object srtStreamID { get; set; }
        public int srtNumLostPackages { get; set; }
        public int srtRetransmitRate { get; set; }
        public double srtRoundTripTime { get; set; }
        public int srtMaxBandwidth { get; set; }
        public int srtNegotiatedLatency { get; set; }
        public string srtEncryption { get; set; }
        public double srtPacketLossRate { get; set; }
        public string srtDecryptionState { get; set; }
        public int srtBufferLevel { get; set; }
        public List<Connection> connections { get; set; }
    }

}
