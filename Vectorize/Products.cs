using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Search;
using System.Numerics;

namespace Vectorize
{
    public class Products
    {

        private readonly IOpenAiService _openAiService;
        private readonly ICognitiveSearchServiceManagement _cognitiveSearchService;

        public Products(IOpenAiService openAiService, ICognitiveSearchServiceManagement cognitiveSearchService) 
        {
            _openAiService = openAiService;
            _cognitiveSearchService = cognitiveSearchService;
        }

        [FunctionName("Products")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "database",
                containerName: "product",
                StartFromBeginning = true,
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<Product> input,
            ILogger logger)
        {

            if (input != null && input.Count > 0)
            {
                logger.LogInformation("Generating embeddings for " + input.Count + " products");

                foreach (Product item in input)
                {
                    await GenerateProductVector(item, logger);
                }
                
            }
        }

        public async Task GenerateProductVector(Product product, ILogger logger)
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

                logger.LogInformation("Saved vector for product: " + product.name);
            }
            catch (Exception x)
            {
                logger.LogError("Exception while generating vector for [" + product.name + "]: " + x.Message);
            }

        }
    }
}
