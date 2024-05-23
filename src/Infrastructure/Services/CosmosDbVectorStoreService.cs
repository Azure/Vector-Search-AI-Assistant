using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using VectorSearchAiAssistant.Common.Interfaces;
using VectorSearchAiAssistant.Common.Models.Configuration;
using VectorSearchAiAssistant.Service.Exceptions;

namespace VectorSearchAiAssistant.Service.Services
{
    public class CosmosDBVectorStoreService : ICosmosDBVectorStoreService
    {
        private readonly CosmosDBVectorStoreServiceSettings _settings;
        private readonly Database _database;
        private readonly CosmosClient _client;
        private readonly Dictionary<string, Container> _containers = [];

        public CosmosDBVectorStoreService(
            IOptions<CosmosDBVectorStoreServiceSettings> options)
        {
            _settings = options.Value;
            _client = new CosmosClient(_settings.Endpoint, _settings.Key, new CosmosClientOptions 
            { 
                ConnectionMode = ConnectionMode.Gateway,
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                }
            });
            _database = _client.GetDatabase(_settings.Database);
        }

        public async Task CreateCollection(string collectionName, CancellationToken cancellationToken = default)
        {
            var throughputProperties = ThroughputProperties.CreateAutoscaleThroughput(4000);

            // Define new container properties including the vector indexing policy
            ContainerProperties properties = new(id: collectionName, partitionKeyPath: "/partitionKey")
            {
                // Define the vector embedding container policy
                VectorEmbeddingPolicy = new(
                new Collection<Embedding>(
                [
                    new Embedding()
                    {
                        Path = "/embedding",
                        DataType = VectorDataType.Float32,
                        DistanceFunction = DistanceFunction.Cosine,
                        Dimensions = 1536
                    }
                ])),
                IndexingPolicy = new IndexingPolicy()
                {
                    // Define the vector index policy
                    VectorIndexes =
                    [
                        new VectorIndexPath()
                        {
                            Path = "/embedding",
                            Type = VectorIndexType.QuantizedFlat
                        }
                    ]
                }
            };

            // Create the container
            var response = await _database.CreateContainerIfNotExistsAsync(properties, throughputProperties, cancellationToken: cancellationToken);

            _containers.Add(collectionName, response.Container);
        }

        public async Task DeleteCollection(string collectionName, CancellationToken cancellationToken = default)
        {
            if (_containers.TryGetValue(collectionName, out var container))
            {
                await container.DeleteContainerAsync(cancellationToken: cancellationToken);
                _containers.Remove(collectionName);
            }
        }

        public bool CollectionExists(string collectionName) =>
            _containers.ContainsKey(collectionName);

        public List<string> GetCollections() =>
            _containers.Keys.ToList();

        public async Task<T> UpsertItem<T>(string collectionName, T item) where T : class
        {
            if (_containers.TryGetValue(collectionName, out var container))
            {
                    return await container.UpsertItemAsync<T>(item);
            }
            else
                throw new CosmosDBException($"The collection {collectionName} is not available.");
        }

        public async IAsyncEnumerable<T> GetNearestRecords<T>(string collectionName, float[] embedding, double similarityScore, int topN) where T : class
        {
            if (_containers.TryGetValue(collectionName, out var container))
            {
                string queryText = "SELECT Top @topN x.id, x.partitionKey, x.metadata, x.similarityScore FROM(SELECT c.id, c.partitionKey, c.metadata, VectorDistance(c.embedding, @embedding, false) as similarityScore FROM c) x WHERE x.similarityScore > @similarityScore ORDER BY x.similarityScore desc";

                var queryDef = new QueryDefinition(
                        query: queryText)
                    .WithParameter("@topN", topN)
                    .WithParameter("@embedding", embedding)
                    .WithParameter("@similarityScore", similarityScore);

                using FeedIterator<T> resultSet = container.GetItemQueryIterator<T>(queryDefinition: queryDef);

                while (resultSet.HasMoreResults)
                {
                    FeedResponse<T> response = await resultSet.ReadNextAsync();

                    foreach (T item in response)
                    {
                        yield return item;
                    }
                }
            }
            else
                throw new CosmosDBException($"The collection {collectionName} is not available.");
        }
    }
}
