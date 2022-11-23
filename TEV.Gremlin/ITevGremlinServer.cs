using Gremlin.Net.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.Gremlin
{
    public interface ITevGremlinServer
    {
        GremlinServer GremlinServer { get; set; }
    }
}
