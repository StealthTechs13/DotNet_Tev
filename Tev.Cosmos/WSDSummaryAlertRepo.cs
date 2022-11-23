using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.Cosmos
{
    public class WSDSummaryAlertRepo: IWSDSummaryAlertRepo
    {
        private readonly ICosmosDbService<WSDSummaryEntity> _cosmosClient;

        public WSDSummaryAlertRepo(ICosmosDbService<WSDSummaryEntity> cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        public async Task<List<WSDSummaryEntity>> GetDeviceSummaryAlerts(string orgId, string deviceId)
        {
            var queryDef = new QueryDefinition($"SELECT c.smokeValue, c.smokeStatus,c.batteryValue,c.batteryStatus," +
            $"c.occurenceTimestamp as enqueuedTimestamp,c.alertType,c.testId FROM c where c.deviceId='{deviceId}' order by c.occurenceTimestamp desc");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.ToList();
        }
    }
}
