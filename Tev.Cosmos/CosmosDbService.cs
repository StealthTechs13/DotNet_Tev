using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tev.Cosmos
{
    public class CosmosDbService<T> : ICosmosDbService<T>
    {
        private readonly Container _container;

        public CosmosDbService(
            CosmosClient dbClient,
            string databaseName,
            string containerName)
        {
            if(dbClient != null)
            {
                this._container = dbClient.GetContainer(databaseName, containerName);
            }
        }

        public async Task AddItemAsync(T item, string partitionKey)
        {
            await this._container.CreateItemAsync<T>(item, new PartitionKey(partitionKey)).ConfigureAwait(false);
        }

        public async Task DeleteItemAsync(string id, string partitionKey)
        {
            await this._container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey)).ConfigureAwait(false);
        }

        public async Task<T> GetItemAsync(string id, string partitionKey)
        {
            try
            {
                ItemResponse<T> response = await this._container.ReadItemAsync<T>(id, new PartitionKey(partitionKey)).ConfigureAwait(false);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return default;
            }

        }

        public async Task<IEnumerable<T>> GetItemsAsync(QueryDefinition queryDef, string partitionKey)
        {
            var query = this._container.GetItemQueryIterator<T>(queryDef, null,new QueryRequestOptions {PartitionKey= new PartitionKey(partitionKey) });
            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync().ConfigureAwait(false);

                results.AddRange(response.Resource);
            }

            return results;
        }

        public async Task UpdateItemAsync(T item, string partitionKey)
        {
            try
            {
                await this._container.UpsertItemAsync(item, new PartitionKey(partitionKey)).ConfigureAwait(false);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return;
            }
        }
    }
}
