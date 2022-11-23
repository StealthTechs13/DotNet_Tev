using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;
using Tev.Cosmos.IRepository;

namespace Tev.Cosmos.Repository
{
    public class FirmwareUpdateHistoryRepo : IFirmwareUpdateHistoryRepo
    {
        private readonly ICosmosDbService<FirmwareUpdateHistory> _cosmosClient;

        public FirmwareUpdateHistoryRepo(ICosmosDbService<FirmwareUpdateHistory> cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }
        public async Task<List<FirmwareUpdateHistory>> GetFirmwareUpdateHisotry(string deviceId, string orgId)
        {
            var queryDef = new QueryDefinition($"SELECT c.version,c.updatedTime FROM c where (c.deviceType = 'TEV' or c.deviceType = 'TEV2') and c.orgId = '{orgId}' and c.deviceId = '{deviceId}'  order by c.updatedTime desc");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.ToList();
        }
    }
}
