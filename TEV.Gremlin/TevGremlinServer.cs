using Gremlin.Net.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.Gremlin
{
    public class TevGremlinServer:ITevGremlinServer
    {

        public TevGremlinServer(string database,string host,string primaryKey,string container,int port)
        {
            
            string containerLink = "/dbs/" + database + "/colls/" + container;
            this.GremlinServer = new GremlinServer(host, port, enableSsl: true,
                                                    username: containerLink,
                                                    password: primaryKey);
        }
        public GremlinServer GremlinServer { get; set; }
    }
}
