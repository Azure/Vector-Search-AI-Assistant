using Azure.Search.Documents.Indexes;
using Azure.Search.Documents;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Core.Serialization;
using System.Text.Json;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace VectorSearchAiAssistant.SemanticKernel.MemorySource
{
    public class AzureCognitiveSearchMemorySource : IMemorySource
    {
        private readonly SearchIndexClient _adminClient;
        private SearchClient _searchClient;
        private readonly AzureCognitiveSearchMemorySourceSettings _settings;
        private readonly ILogger _logger;

        private AzureCognitiveSearchMemorySourceConfig _config;

        public AzureCognitiveSearchMemorySource(
            IOptions<AzureCognitiveSearchMemorySourceSettings> settings,
            ILogger<AzureCognitiveSearchMemorySource> logger)
        {
            _settings = settings.Value;
            AzureKeyCredential credentials = new(_settings.Key);

            _adminClient = new SearchIndexClient(new Uri(_settings.Endpoint), credentials, new SearchClientOptions()
            {
                Serializer = new JsonObjectSerializer(
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })
            });
            _logger = logger;

            // Not initializing _searchClient here because the index might still be creating when this constructor runs.
            // Deferring the initialization to the GetMemories call (by that time, the index should be guranteed to exist).
        }

        public async Task<List<string>> GetMemories()
        {
            EnsureSearchClient();
            await EnsureConfig();

            var memories = new List<string>();

            foreach (var memorySource in _config.FacetedQueryMemorySources)
            {
                var searchOptions = new SearchOptions
                {
                    Filter = memorySource.Filter,
                    Size = 0
                };
                foreach (var facet in memorySource.Facets)
                    searchOptions.Facets.Add(facet.Facet);

                var facetTemplates = memorySource.Facets.ToDictionary(f => f.Facet.Split(',')[0], f => f.CountMemoryTemplate);

                var result = await _searchClient
                    .SearchAsync<SearchDocument>("*", searchOptions);

                long totalCount = 0;
                foreach (var facet in result.Value.Facets)
                {
                    foreach (var facetResult in facet.Value)
                    {
                        memories.Add(string.Format(
                            facetTemplates[facet.Key],
                            facetResult.Value, facetResult.Count));
                        totalCount += facetResult.Count.Value;
                    }
                }

                memories.Add(string.Format(memorySource.TotalCountMemoryTemplate, totalCount));
            }

            return memories;
        }

        private void EnsureSearchClient()
        {
            if (_searchClient == null)
                _searchClient = _adminClient.GetSearchClient(_settings.IndexName);
        }

        private async Task EnsureConfig()
        {
            if (_config  == null)
            {
                var blobServiceClient = new BlobServiceClient(_settings.ConfigBlobStorageConnection);
                var storageClient = blobServiceClient.GetBlobContainerClient(_settings.ConfigBlobStorageContainer);
                var blobClient = storageClient.GetBlobClient(_settings.ConfigFilePath);
                var reader = new StreamReader(await blobClient.OpenReadAsync());
                var configContent = await reader.ReadToEndAsync();

                _config = JsonConvert.DeserializeObject<AzureCognitiveSearchMemorySourceConfig>(configContent);
            }
        }
    }
}
