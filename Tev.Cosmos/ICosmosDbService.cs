using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tev.Cosmos
{
    public interface ICosmosDbService<TEntity>
    {
        Task<IEnumerable<TEntity>> GetItemsAsync(QueryDefinition queryDef, string partitionKey);
        Task<TEntity> GetItemAsync(string id, string partitionKey);
        Task AddItemAsync(TEntity item, string partitionKey);
        Task UpdateItemAsync(TEntity item, string partitionKey);
        Task DeleteItemAsync(string id, string partitionKey);
    }
}
