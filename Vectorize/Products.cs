using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using DataCopilot.Vectorize.Models;
using DataCopilot.Vectorize.Services;

namespace DataCopilot.Vectorize
{
    public class Products
    {

        private OpenAI _openAI;
        private Redis _redis;

        [FunctionName("Products")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "database",
                containerName: "product",
                StartFromBeginning = true,
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<Product> input,
            [CosmosDB(
                databaseName: "database",
                containerName: "embedding",
                Connection = "CosmosDBConnection")]IAsyncCollector<DocumentVector> output,
            ILogger log)
        {

            _redis = new Redis(log);

            await _redis.CreateRedisIndex();

            _openAI = new OpenAI();

            if (input != null && input.Count > 0)
            {
                log.LogInformation("Generating embeddings for " + input.Count + " products");
                try
                {
                    foreach (Product item in input)
                    {
                         await GenerateProductVectors(item, output, log);
                    }
                }
                finally
                {
                }
            }
        }

        public async Task GenerateProductVectors(Product product, IAsyncCollector<DocumentVector> output, ILogger log)
        {
            //Serialize the product object to send to OpenAI
            string sProduct = JObject.FromObject(product).ToString();

            DocumentVector documentVector = new DocumentVector(product.id, product.categoryId, "product");
            
            try
            {
                //Get the embeddings from OpenAI
                documentVector.vector = await _openAI.GetEmbeddingsAsync(sProduct, log);

                //Save to Cosmos DB
                await output.AddAsync(documentVector);

                //Save to Redis Cache
                await _redis.CacheVector(documentVector, log);

                log.LogInformation("Cached embeddings for product: " + product.name);
            }
            catch (Exception x)
            {
                log.LogError("Exception while generating embeddings for [" + product.name + "]: " + x.Message);
            }

        }
    }
}
