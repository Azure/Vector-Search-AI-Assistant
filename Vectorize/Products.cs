using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Vectorize.Services;
using Vectorize.Models;

namespace Vectorize
{
    public class Products
    {

        private OpenAiService _openAI = new OpenAiService();
        private MongoDBService _mongo = new MongoDBService();

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
            //Serialize the product object to send to OpenAI
            string sProduct = JObject.FromObject(product).ToString();

            
            try
            {
                //Get the embeddings from OpenAI
                product.vector = await _openAI.GetEmbeddingsAsync(sProduct, logger);

                
                //Save to Mongo
                await _mongo.UpsertVector(product, logger);

                logger.LogInformation("Saved vector for product: " + product.name);
            }
            catch (Exception x)
            {
                logger.LogError("Exception while generating vector for [" + product.name + "]: " + x.Message);
            }

        }
    }
}
