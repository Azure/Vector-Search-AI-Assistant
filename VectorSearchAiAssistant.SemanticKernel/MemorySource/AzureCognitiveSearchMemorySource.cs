using Azure.Search.Documents.Indexes;
using Azure.Search.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using System.Collections.Concurrent;
using Azure;

namespace VectorSearchAiAssistant.SemanticKernel.MemorySource
{
    public class AzureCognitiveSearchMemorySource : IMemorySource
    {
        private readonly SearchIndexClient _adminClient;
        private SearchClient _searchClient;
        private readonly string _searchIndexName;

        private readonly ILogger _logger;

        public AzureCognitiveSearchMemorySource(string endpoint, string apiKey, string indexName, ILogger logger)
        {
            AzureKeyCredential credentials = new(apiKey);

            //_adminClient = new SearchIndexClient(new Uri(endpoint), credentials, GetSearchClientOptions());
            //_searchIndexName = indexName;
            //_textEmbedding = textEmbedding;
            //_logger = logger;
        }

        public async Task<List<string>> GetMemories()
        {
            throw new NotImplementedException();
        }
    }
}
