using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.Cosmos
{
    public class DeviceSetupRepo : IDeviceSetupRepo
    {
        private readonly ICosmosDbService<DeviceSetup> _cosmosClient;

        public DeviceSetupRepo(ICosmosDbService<DeviceSetup> cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }
        public async Task<DeviceSetup> GetDeviceSetupStatus(string logicalDeviceId, string orgId)
        {
            //var result = await _cosmosClient.GetItemAsync(logicalDeviceId, orgId).ConfigureAwait(false);
            QueryDefinition queryDef = new QueryDefinition($"SELECT * FROM c where c.logicalDeviceId = '{logicalDeviceId}'  order by c._ts desc offset 0 limit 1");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId);
            return result.FirstOrDefault();
        }
    }
}
