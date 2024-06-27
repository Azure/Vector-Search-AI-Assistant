using BuildYourOwnCopilot.Common.Models.Configuration;
using BuildYourOwnCopilot.Infrastructure.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace BuildYourOwnCopilot.Infrastructure.Services
{
    public class CosmosDBClientFactory(
        IOptions<CosmosDBSettings> settings) : ICosmosDBClientFactory
    {
        private readonly CosmosDBSettings _settings = settings.Value;

        private readonly CosmosClient _client = new CosmosClient(
            settings.Value.Endpoint,
            settings.Value.Key,
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                }
            });

        public CosmosClient Client => _client;

        public string DatabaseName => _settings.Database;
    }
}
