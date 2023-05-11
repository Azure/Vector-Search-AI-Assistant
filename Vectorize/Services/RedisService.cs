using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using DataCopilot.Vectorize.Utils;
using DataCopilot.Vectorize.Models;
using Microsoft.Azure.Cosmos;

namespace DataCopilot.Vectorize.Services
{
    // Redis Cache for Embeddings
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

        
        string? _errorMessage;
        List<string> _statusMessages = new();


        void ClearState()
        {
            _errorMessage = "";
            _statusMessages.Clear();
        }

        public async Task CreateRedisIndex(CosmosClient cosmosClient)
        {
            ClearState();

            try
            {
                _logger.LogInformation("Checking if Redis index exists...");
                

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
                await ResetCache(_logger);

                _logger.LogInformation("Creating Redis index...");

                var _ = await _database.ExecuteAsync("FT.CREATE",
                    "embeddingIndex", "SCHEMA", "vector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT32", "DISTANCE_METRIC", "COSINE", "DIM", "1536");
                _logger.LogInformation("Created Redis index for embeddings. Repopulating from Cosmos DB...");

                //Repopulate from Cosmos DB if there are any embeddings there
                await RestoreRedisStateFromCosmosDB(cosmosClient);
                _logger.LogInformation("Repopulated Redis index from Cosmos DB embeddings");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                _logger.LogError(_errorMessage);
            }
        }

        public async Task CacheVector(DocumentVector documentVector)
        {
            try
            {
                // Perform cache operations using the cache object...
                _logger.LogInformation("Submitting embedding to cache");

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
                _logger.LogError(_errorMessage);
            }
        }
        
        async Task ResetCache(ILogger log)
        {
            ClearState();

            try
            {
                _logger.LogInformation("Deleting all redis keys...");
                var db = _connectionMultiplexer.GetDatabase();
                var _ = await db.ExecuteAsync("FLUSHDB");
                log.LogInformation("Done.");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                _logger.LogError(_errorMessage);
            }
        }

        async Task RestoreRedisStateFromCosmosDB(CosmosClient cosmosClient)
        {
            ClearState();

            
            try
            {
                _logger.LogInformation("Processing documents...");
                await foreach (var doc in GetAllEmbeddings(cosmosClient))
                {
                    await CacheVector(doc);
                    _logger.LogInformation($"\tCached embedding for document with id '{doc.itemId}'");
                }
                _logger.LogInformation("Done.");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                _logger.LogError(_errorMessage);
            }
        }

        public async IAsyncEnumerable<DocumentVector> GetAllEmbeddings(CosmosClient cosmosClient)
        {
            var container = cosmosClient.GetContainer("database", "embedding");

            using var feedIterator = container.GetItemQueryIterator<DocumentVector>("SELECT * FROM c");

            while (feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();
                foreach (var item in response)
                {
                    yield return item;
                }
            }
        }

        public void Dispose()
        {
            _connectionMultiplexer.Dispose();
        }
    }
}
