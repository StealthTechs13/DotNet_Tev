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
    public class DeviceRepo : IDeviceRepo
    {
        private readonly ICosmosDbService<Device> _cosmosClient;

        public DeviceRepo(ICosmosDbService<Device> cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        public async Task<Device> GetDevice(string logicalDeviceId, string orgId)
        {
            var queryDef = new QueryDefinition($"select * from c where c.logicalDeviceId = '{logicalDeviceId}'");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.FirstOrDefault();
        }

        public async Task<List<Device>> GetDevicesByLocation(string locationId, string orgId)
        {
            var queryDef = new QueryDefinition($"select * from c where c.locationId = '{locationId}'");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.ToList();
        }

        public async Task<List<Device>> GetDeviceByOrgId(string orgId)
        {
            var queryDef = new QueryDefinition($"select * from c where c.orgId = '{orgId}'");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.ToList();
        }

        public async Task<List<Device>> GetTev2DeviceByOrgId(string orgId)
        {
            var queryDef = new QueryDefinition($"select * from c where c.orgId = '{orgId}'and c.deviceType ='TEV2'");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.ToList();
        }

        public async Task<List<Device>> GetDeviceByDeviceIds(List<string> logicalDeviceIds, string orgId)
        {
            string sqlQuery = string.Empty;
            if (logicalDeviceIds != null)
            {
                var quotesDeviceIds = logicalDeviceIds.Select(x => $"'{x}'").ToList();
                var str = string.Join(',', quotesDeviceIds);
                sqlQuery = $"select * from c where c.logicalDeviceId in ({str})";
            }
            var queryDef = new QueryDefinition(sqlQuery);
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.ToList();

        }

        public async Task<List<Device>> GetTev2DeviceByDeviceIds(List<string> logicalDeviceIds, string orgId)
        {
            string sqlQuery = string.Empty;
            if (logicalDeviceIds != null)
            {
                var quotesDeviceIds = logicalDeviceIds.Select(x => $"'{x}'").ToList();
                var str = string.Join(',', quotesDeviceIds);
                sqlQuery = $"select * from c where c.logicalDeviceId in ({str}) and c.deviceType ='TEV2'";
            }
            var queryDef = new QueryDefinition(sqlQuery);
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.ToList();

        }
        public async Task<FeatureConfig> GetDeviceFeatureConfiguration(string logicalDeviceId, string orgId)
        {
            var queryDef = new QueryDefinition($"select * from c where c.logicalDeviceId = '{logicalDeviceId}'");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return result.FirstOrDefault().FeatureConfig;
        }

        public async Task DeleteDevice(string logicalDeviceId, string orgId)
        {
            QueryDefinition queryDefinition = new QueryDefinition($"select * from c where c.logicalDeviceId = '{logicalDeviceId}'");

            var result = await _cosmosClient.GetItemsAsync(queryDefinition, orgId).ConfigureAwait(false);

            foreach (Device item in result.ToList())
            {
                await _cosmosClient.DeleteItemAsync(item.Id, orgId).ConfigureAwait(false);
            }
        }

        public async Task UpdateDevice(string orgId, Device device)
        {
            await _cosmosClient.UpdateItemAsync(device, orgId);
        }

        public async Task<bool> DeviceBelongsToOrg(string logicalDeviceId, string orgId)
        {
            var queryDef = new QueryDefinition($"select * from c where c.logicalDeviceId = '{logicalDeviceId}'");
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);

            if (result.ToList().Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<Device> GetDeviceBySubscription(string subscriptionId)
        {
            var queryDef = new QueryDefinition($"SELECT * FROM c where c.subscription.subscriptionId = '{subscriptionId}'");
            var result = await _cosmosClient.GetItemsAsync(queryDef, null).ConfigureAwait(false);
            return result.FirstOrDefault();
        }

        public async Task<string> GetLatestFirmwareVersion(string deviceType)
        {
            var queryDef = new QueryDefinition($"SELECT c.currentFirmwareVersion FROM c where c.orgId = '0' and c.deviceType = '{deviceType}' ");
            var result = await _cosmosClient.GetItemsAsync(queryDef, "0").ConfigureAwait(false);
            if(result.ToList().Count > 0)
            {
                return result.FirstOrDefault().CurrentFirmwareVersion.ToString();
            }
            return null;
        }

    }
}
