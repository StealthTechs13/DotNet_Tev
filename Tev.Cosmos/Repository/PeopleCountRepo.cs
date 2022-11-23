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
    public class PeopleCountRepo: IPeopleCountRepo
    {
        private readonly ICosmosDbService<PeopleCount> _cosmosClient;

        public PeopleCountRepo(ICosmosDbService<PeopleCount> cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }
        public async Task<List<PeopleCount>> GetPeopleCount(int skip, int take, string deviceId,string orgId)
        {
            var queryDef = new QueryDefinition($"SELECT c.peopleCount,c.occurenceTimestamp FROM c " +
            $"where c.telemetryType = 'summary' and c.deviceType = 'TEV' and c.orgId = '{orgId}' and c.deviceId = '{deviceId}'  " +
            $"order by c.occurenceTimestamp desc offset {skip} limit {take}");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.ToList();
        }
    }
}
