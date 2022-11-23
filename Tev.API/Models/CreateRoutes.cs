using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class CreateRoutes
    {
        public string action { get; set; }
        public string deviceID { get; set; }
        public string elementType { get; set; }
        public Routes fields { get; set; }
    }
    public class Destination
    {
        public string name { get; set; }
        public string id { get; set; }
        public string protocol { get; set; }
        public int port { get; set; }
        public string address { get; set; }
        public string networkInterface { get; set; }
        public string action { get; set; }
        public string srtEncryption { get; set; }
        public string srtPassPhrase { get; set; }
        public int srtLatency { get; set; }
        public string srtMode { get; set; }
        public string srtGroupMode { get; set; }
        public string srtNetworkBondingParams { get; set; }
    }

    public class Routes
    {
        public string name { get; set; }
        public bool startRoute { get; set; }
        public Source source { get; set; }
        public List<Destination> destinations { get; set; }
    }



    public class Source
    {
        public string name { get; set; }
        public string id { get; set; }
        public string address { get; set; }
        public string protocol { get; set; }
        public int port { get; set; }
        public string networkInterface { get; set; }
        public string srtPassPhrase { get; set; }
        public long srtRcvBuf { get; set; }
        public int srtLatency { get; set; }
        public string srtMode { get; set; }
        public string srtGroupMode { get; set; }
        public string srtNetworkBondingParams { get; set; }
    }

}
