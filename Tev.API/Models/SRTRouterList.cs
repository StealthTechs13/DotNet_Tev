using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class SRTRouterList
    {
        public int numResults { get; set; }
        public int numPages { get; set; }
        public List<Datum> data { get; set; }
        public int numActiveOutputConnections { get; set; }
    }

    public class RouterReq
    {
        public string action { get; set; }
        public string deviceID { get; set; }
        public string elementType { get; set; }
        public Field fields { get; set; }
    }
    public class Field
    {
        public int name { get; set; }
        public int startRoute { get; set; }
        public SourcePort source { get; set; }
        public List<DestinationPort> destinations { get; set; }
    }
    public class Datum
    {
        public string name { get; set; }
        public string elapsedTime { get; set; }
        public string id { get; set; }
        public string label { get; set; }
        public string state { get; set; }
        public SourcePort source { get; set; }
        public List<DestinationPort> destinations { get; set; }
        public string summaryStatusCode { get; set; }
        public string summaryStatusDetails { get; set; }
    }

    public class DestinationPort
    {
        public string name { get; set; }
        public string id { get; set; }
        public string protocol { get; set; }
        public int port { get; set; }
        public bool started { get; set; }
        public string mode { get; set; }
        public object networkAddress { get; set; }
        public string networkInterface { get; set; }
        public string address { get; set; }
        public int ttl { get; set; }
        public int mtu { get; set; }
        public int tos { get; set; }
        public string state { get; set; }
        public string srtEncryption { get; set; }
        public int srtLatency { get; set; }
        public int srtConnectionLimit { get; set; }
        public string srtOverhead { get; set; }
        public string srtPassPhrase { get; set; }
        public bool retainHeader { get; set; }
        public string srtGroupMode { get; set; }
        public string summaryStatusCode { get; set; }
        public string summaryStatusDetails { get; set; }
    }

    public class SourcePort
    {
        public string name { get; set; }
        public string id { get; set; }
        public string networkInterface { get; set; }
        public string mode { get; set; }
        public string address { get; set; }
        public string protocol { get; set; }
        public int port { get; set; }
        public string encryption { get; set; }
        public string usedBandwidth { get; set; }
        public string state { get; set; }
        public int srtLatency { get; set; }
        public int srtRcvBuf { get; set; }
        public string srtPassPhrase { get; set; }
        public string srtGroupMode { get; set; }
        public string summaryStatusCode { get; set; }
        public string summaryStatusDetails { get; set; }
    }

    public class SRTRoute
    {
        public string name { get; set; }
        public string id { get; set; }
        public SourcePort source { get; set; }
        public List<DestinationPort> destinations { get; set; }
        public string summaryStatusCode { get; set; }
        public string summaryStatusDetails { get; set; }
        public string state { get; set; }
        public string elapsedTime { get; set; }
        public bool hasPendingDelete { get; set; }
    }

}
