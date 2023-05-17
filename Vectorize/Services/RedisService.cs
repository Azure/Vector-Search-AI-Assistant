using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using DataCopilot.Vectorize.Utils;
using DataCopilot.Vectorize.Models;
using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace DataCopilot.Vectorize.Services
{
    
    public class RedisService : IDisposable
    {
        private readonly string _redisConnectionString;
        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;
        
        private readonly ILogger _logger;

        public RedisService(string redisConnectionString, ILogger logger) 
        { 
            _redisConnectionString = redisConnectionString;
            _logger = logger;

            _connectionMultiplexer = ConnectionMultiplexer.Connect(_redisConnectionString);
            _database = _connectionMultiplexer.GetDatabase();
        }


        public async Task CreateRedisIndex(CosmosClient cosmosClient)
        {

            _logger.LogInformation("Checking if Redis index exists...");


            try
            { 

                RedisResult? index = null;
                try
                {
                    index = await _database.ExecuteAsync("FT.INFO", "embeddingIndex");
                }
                catch (RedisServerException redisX)
                {
                    _logger.LogInformation("Exception while checking embedding index:" + redisX.Message);
                    //not returning - index most likely doesn't exist
                }
                if (index != null)
                {
                    _logger.LogInformation("Redis index for embeddings already exists. Skipping...");
                    return;
                }



                // If index doesn't exist remove all hashes
                await ResetCacheAsync();

                _logger.LogInformation("Creating Redis index...");

                await _database.ExecuteAsync("FT.CREATE",
                    "embeddingIndex", "SCHEMA", "vector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT32", "DISTANCE_METRIC", "COSINE", "DIM", "1536");


                //Repopulate from Cosmos DB if there are any embeddings there
                await RestoreRedisStateFromCosmosDB(cosmosClient);

            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            
        }

        public async Task CacheVector(DocumentVector documentVector)
        {
            try
            {
                // Perform cache operations using the cache object...
                _logger.LogInformation("Submitting vector to cache");

                var mem = new ReadOnlyMemory<float>(documentVector.vector);

                await _database.HashSetAsync(documentVector.id, new[]{
                    new HashEntry("vector", mem.AsBytes()),
                    new HashEntry("itemId", documentVector.itemId),
                    new HashEntry("containerName", documentVector.containerName),
                    new HashEntry("partitionKey", documentVector.partitionKey)
                });

            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex.Message);
            }
        }

        public async Task RemoveVector(string documentVectorId)
        {
            try
            {
                await _database.KeyDeleteAsync(documentVectorId);

                _logger.LogInformation("Vector removed from Redis cache");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        
        async Task ResetCacheAsync()
        {
            
            try
            {
                _logger.LogInformation("Reset cache. Deleting all redis keys...");
                
                await _database.ExecuteAsync("FLUSHDB");
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        async Task RestoreRedisStateFromCosmosDB(CosmosClient cosmosClient)
        {
            
            try
            {
                _logger.LogInformation("Repopulated Redis index from Cosmos DB");

                var container = cosmosClient.GetContainer("database", "embedding");

                using var feedIterator = container.GetItemQueryIterator<DocumentVector>("SELECT * FROM c");

                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<DocumentVector> response = await feedIterator.ReadNextAsync();
                    foreach (DocumentVector item in response)
                    {
                        await CacheVector(item);
                    }
                }
                
                _logger.LogInformation("Repopulate cache complete.");
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex.Message);
            }
        }

        public void Dispose()
        {
            _connectionMultiplexer.Dispose();
        }
    }
}
