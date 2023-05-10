using StackExchange.Redis;
using Search.Utilities;
using Search.Models;
using Search.Constants;

namespace Search.Services
{
    public class RedisService
    {
        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;
        private readonly ILogger _logger;
        private string? _errorMessage;
        private List<string> _statusMessages = new();

        public RedisService(string connection, ILogger logger)
        {
            ArgumentException.ThrowIfNullOrEmpty(connection);


            _connectionMultiplexer = ConnectionMultiplexer.Connect(connection);
            _database = _connectionMultiplexer.GetDatabase();

            _logger = logger;
        }

        public IDatabase GetDatabase()
        {
            return _database;
        }

        void ClearState()
        {
            _errorMessage = "";
            _statusMessages.Clear();
        }

        public async Task CreateRedisIndexAsync()
        {
            ClearState();

            try
            {
                _logger.LogInformation("Checking if Redis index exists...");
                var db = _connectionMultiplexer.GetDatabase();

                RedisResult? index = null;

                try
                {
                    index = await db.ExecuteAsync("FT.INFO", "embeddingIndex");
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

                _logger.LogInformation("Creating Redis index...");

                var _ = await db.ExecuteAsync("FT.CREATE",
                    "embeddingIndex", "SCHEMA", "vector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT32", "DISTANCE_METRIC", "COSINE", "DIM", "1536");

                _logger.LogInformation("Created Redis index for embeddings");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                _logger.LogError(_errorMessage);
            }
        }

        public async Task<List<DocumentVector>> VectorSearchAsync(float[] embeddings)
        {

            List<DocumentVector> retDocs = new List<DocumentVector>();


            int maxResults = 30;
            var resultList = new List<string>(maxResults);
            _errorMessage = "";


            var memory = new ReadOnlyMemory<float>(embeddings);

            //Search Redis for similar embeddings
            var res = await _database.ExecuteAsync("FT.SEARCH",
                "embeddingIndex",
                $"*=>[KNN {maxResults} @vector $BLOB]",
                "PARAMS",
                "2",
                "BLOB",
                memory.AsBytes(),
                "SORTBY",
                "__vector_score",
                "DIALECT",
                "2");

            if (res.Type == ResultType.MultiBulk)
            {
                var results = (RedisResult[])res;
                var count = (int)results[0];

                if ((2 * count + 1) != results.Length)
                {
                    throw new NotSupportedException($"Unexpected entries is Redis result, '{results.Length}' results for count of '{count}'");
                }


                for (var i = 0; i < count; i++)
                {
                    //fetch the RedisResult
                    RedisResult[] result = (RedisResult[])results[(2 * i) + 1 + 1];

                    if (result == null)
                        continue;

                    string itemId = "", partitionKey = "", containerName = "";


                    for (int j = 0; j < result.Length; j += 2)
                    {
                        var key = (string)result[j];
                        switch (key)
                        {
                            case "partitionKey":
                                partitionKey = (string)result[j + 1];
                                break;

                            case "itemId":
                                itemId = (string)result[j + 1];
                                break;

                            case "containerName":
                                containerName = (string)result[j + 1];
                                break;
                        }
                    }

                    retDocs.Add(new DocumentVector(itemId, partitionKey, containerName));

                }

            }
            else
            {
                throw new NotSupportedException($"Unexpected query result type {res.Type}");
            }

            return retDocs;
        }

    }
}
