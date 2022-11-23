using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.Cosmos
{
    public class GeneralRepo: IGeneralRepo
    {
        private readonly ICosmosDbService<AlertCount> _cosmosClient;

        public GeneralRepo(ICosmosDbService<AlertCount> cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }
        public async Task<List<(string DeviceId, int Count)>> GetUnacknowledgedAlerts(List<string> deviceIds, string orgId)
        {
            var listNew = deviceIds.Select(x => $"'{x}'").ToList();
            var deviceIdsStr = $"({string.Join(',', listNew)})";
            var queryDef = new QueryDefinition($"SELECT count(1) as count,c.deviceId FROM c where c.acknowledged=false and (NOT is_defined(c.isDeleted) OR c.isDeleted <> true OR ISNULL(c.isDeleted ) = true) and c.deviceId in {deviceIdsStr} group by c.deviceId");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.Select(x => ((string)x.DeviceId, (int)x.Count)).ToList();
        }
    }
}
