using StackExchange.Redis;
using DataCopilot.Search.Utilities;
using DataCopilot.Search.Models;
using DataCopilot.Search.Constants;
using Vectorize.Models;

namespace DataCopilot.Search.Services
{
    public class RedisService
    {
        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;
        private readonly ILogger _logger;


        public RedisService(string connectionString, ILogger logger)
        {
            ArgumentException.ThrowIfNullOrEmpty(connectionString);


            _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            _database = _connectionMultiplexer.GetDatabase();

            _logger = logger;
        }


        public async Task<List<DocumentVector>> VectorSearchAsync(float[] embeddings)
        {

            List<DocumentVector> retDocs = new List<DocumentVector>();


            int maxResults = 30;
            var resultList = new List<string>(maxResults);
            

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
