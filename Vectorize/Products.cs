using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Search;
using Microsoft.Azure.Functions.Worker;

namespace Vectorize
{
    public class Products
    {

        private readonly IOpenAiService _openAiService;
        private readonly ICognitiveSearchServiceManagement _cognitiveSearchService;
        private readonly ILogger _logger;

        public Products(IOpenAiService openAiService, ICognitiveSearchServiceManagement cognitiveSearchService,
            ILoggerFactory loggerFactory)
        {
            _openAiService = openAiService;
            _cognitiveSearchService = cognitiveSearchService;
            _logger = loggerFactory.CreateLogger<Products>();
        }

        [Function("Products")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "database",
                containerName: "product",
                StartFromBeginning = true,
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<Product> input)
        {

            if (input != null && input.Count > 0)
            {
                _logger.LogInformation("Generating embeddings for " + input.Count + " products");

                foreach (var item in input)
                {
                    await GenerateProductVector(item);
                }
                
            }
        }

        public async Task GenerateProductVector(Product product)
        {
            // Serialize the product object to send to OpenAI
            var sProduct = JObject.FromObject(product).ToString();
            
            try
            {
                // Get the embeddings from OpenAI
                var(vector, responseTokens) = await _openAiService.GetEmbeddingsAsync(sProduct);
                product.vector = vector;

                // Save to Cognitive Search
                await _cognitiveSearchService.InsertVector(product);

                _logger.LogInformation("Saved vector for product: " + product.name);
            }
            catch (Exception x)
            {
                _logger.LogError("Exception while generating vector for [" + product.name + "]: " + x.Message);
            }

        }
    }
}
