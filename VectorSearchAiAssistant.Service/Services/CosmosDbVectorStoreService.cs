using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using VectorSearchAiAssistant.Common.Interfaces;
using VectorSearchAiAssistant.Common.Models.Configuration;

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
            _client = new CosmosClient(_settings.Endpoint, _settings.Key);
            _database = _client.GetDatabase(_settings.Database);
        }

        public async Task CreateCollection(string collectionName, CancellationToken cancellationToken = default)
        {
            var throughputProperties = ThroughputProperties.CreateAutoscaleThroughput(4000);

            // Define new container properties including the vector indexing policy
            ContainerProperties properties = new(id: collectionName, partitionKeyPath: "/id")
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
    }
}
