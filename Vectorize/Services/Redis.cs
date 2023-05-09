using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using DataCopilot.Vectorize.Utils;
using DataCopilot.Vectorize.Models;
using Microsoft.Azure.Cosmos;

namespace DataCopilot.Vectorize.Services
{
    // Redis Cache for Embeddings
    public class Redis : IDisposable
    {
        private static readonly ConnectionMultiplexer _connectionMultiplexer = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("RedisConnection"));
        ILogger log;
        string? _errorMessage;
        List<string> _statusMessages = new();
        CosmosDB _cosmosDB = new CosmosDB(Environment.GetEnvironmentVariable("CosmosDBConnection"));

        public Redis(ILogger log)
        {
            this.log = log;
            //CreateRedisIndex().Wait();
        }

        public IDatabase GetDatabase()
        {
            return _connectionMultiplexer.GetDatabase();
        }

        void ClearState()
        {
            _errorMessage = "";
            _statusMessages.Clear();
        }

        public async Task CreateRedisIndex()
        {
            ClearState();

            try
            {
                log.LogInformation("Checking if Redis index exists...");
                var db = _connectionMultiplexer.GetDatabase();

                RedisResult index = null;
                try
                {
                     index = await db.ExecuteAsync("FT.INFO", "embeddingIndex");
                }
                catch (RedisServerException redisX)
                {
                    log.LogInformation("Exception while checking embedding index:" + redisX.Message);
                    //not returning - index most likely doesn't exist
                }
                if (index != null)
                {
                    log.LogInformation("Redis index for embeddings already exists. Skipping...");
                    return;
                }

                // If index doesn't exist remove all hashes
                await ResetCache(log);

                log.LogInformation("Creating Redis index...");

                var _ = await db.ExecuteAsync("FT.CREATE",
                    "embeddingIndex", "SCHEMA", "vector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT32", "DISTANCE_METRIC", "COSINE", "DIM", "1536");
                log.LogInformation("Created Redis index for embeddings. Repopulating from Cosmos DB...");

                //Repopulate from Cosmos DB if there are any embeddings there
                await RestoreRedisStateFromCosmosDB(log);
                log.LogInformation("Repopulated Redis index from Cosmos DB embeddings");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                log.LogError(_errorMessage);
            }
        }

        public async Task CacheVector(DocumentVector documentVector, ILogger log)
        {
            try
            {
                // Perform cache operations using the cache object...
                log.LogInformation("Submitting embedding to cache");

                var db = _connectionMultiplexer.GetDatabase();

                var mem = new ReadOnlyMemory<float>(documentVector.vector);

                await db.HashSetAsync(documentVector.id, new[]{
                    new HashEntry("vector", mem.AsBytes()),
                    new HashEntry("itemId", documentVector.itemId),
                    new HashEntry("containerName", documentVector.containerName),
                    new HashEntry("partitionKey", documentVector.partitionKey)
                });

            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                log.LogError(_errorMessage);
            }
        }
        
        async Task ResetCache(ILogger log)
        {
            ClearState();

            try
            {
                log.LogInformation("Deleting all redis keys...");
                var db = _connectionMultiplexer.GetDatabase();
                var _ = await db.ExecuteAsync("FLUSHDB");
                log.LogInformation("Done.");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                log.LogError(_errorMessage);
            }
        }

        async Task RestoreRedisStateFromCosmosDB(ILogger log)
        {
            ClearState();

            try
            {
                log.LogInformation("Processing documents...");
                await foreach (var doc in _cosmosDB.GetAllEmbeddings())
                {
                    await CacheVector(doc, log);
                    log.LogInformation($"\tCached embedding for document with id '{doc.itemId}'");
                }
                log.LogInformation("Done.");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                log.LogError(_errorMessage);
            }
        }

        public void Dispose()
        {
            _connectionMultiplexer.Dispose();
        }
    }
}
