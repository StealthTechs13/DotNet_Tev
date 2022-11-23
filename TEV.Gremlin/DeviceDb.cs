using Gremlin.Net.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Gremlin.Net;

namespace Tev.Gremlin
{
    public class DeviceDb : IDeviceDb
    {
        private GremlinClient gremlinClient;
        public DeviceDb(ITevGremlinClient client)
        {
            gremlinClient = client.GremlinClient;
        }
        public async Task<ResultSet<dynamic>> AddDevice(string logicalDeviceId, string siteId, string orgId)
        {
            var result = await gremlinClient.SubmitAsync<dynamic>($"g.addV('device').property('id','{logicalDeviceId}').property('orgId','{orgId}').as('m').V('{siteId}').addE('child').to('m')");
            return result;
        }

        public async Task<ResultSet<dynamic>> GetDevices(string parentSite, bool recursive = false)
        {
            if (!recursive)
            {
                var result1 = await gremlinClient.SubmitAsync<dynamic>($"g.V('{parentSite}').out('child').hasLabel('device')");
                return result1;
            }
            var result = await gremlinClient.SubmitAsync<dynamic>($"g.V('{parentSite}').repeat(out('child')).emit().hasLabel('device')");
            return result;
        }
    }
}
