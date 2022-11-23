using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.Cosmos;
using Microsoft.Azure.Cosmos;

namespace Tev.API
{
    public static class CosmosConfig<T>
    {
        //todo add retry mechaninsm in cosmos client and explore cosmosclient options
        public static async Task<CosmosDbService<T>> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection,string containerName)
        {
                string databaseName;
                string conString;

                if (configurationSection != null)
                {
                    databaseName = configurationSection.GetSection("TevViolationTelemetryDbName").Value;
                    conString = configurationSection.GetSection("TevConnectionString").Value;

                    CosmosClient client = new CosmosClient(conString, new CosmosClientOptions
                    {
                        ConnectionMode = ConnectionMode.Direct,
                        SerializerOptions = new CosmosSerializationOptions
                        {
                            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                        }
                    });

                    CosmosDbService<T> cosmosDbService = new CosmosDbService<T>(client, databaseName, containerName);
                    DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName).ConfigureAwait(false);
                    await database.Database.CreateContainerIfNotExistsAsync(containerName, "/orgId").ConfigureAwait(false);

                    return cosmosDbService;
                }
                else
                {
                throw new ArgumentNullException("configurationSection");
                }
        }
    }
}
