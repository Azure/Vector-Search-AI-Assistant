using DataCopilot.Vectorize.Models;
using DataCopilot.Vectorize.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Container = Microsoft.Azure.Cosmos.Container;

namespace Datacopilot.Vectorize
{
    public class AddRemoveData
    {

        private readonly RedisService _redis;

        public AddRemoveData(RedisService redis)
        {
            _redis = redis;
        }


        [FunctionName("AddRemoveData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [CosmosDB(
                Connection = "CosmosDBConnection")]CosmosClient cosmosClient,
            ILogger logger)
        {
            logger.LogInformation("C# HTTP trigger function processed a request.");

            string? action = req.Query["action"];

            try
            {

                if (action == "add")
                {
                    await AddProduct(cosmosClient, logger);
                }
                else if (action == "remove")
                {
                    await RemoveProduct(cosmosClient, logger);

                }
                else
                {
                    throw new Exception("Bad Request: Missing value for action in query string, add or remove");
                }

                string responseMessage = "HTTP trigger function executed successfully.";

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {

                return new BadRequestObjectResult(ex);

            }
        }

        public async Task AddProduct(CosmosClient cosmosClient, ILogger logger)
        {

            try
            {

                Container container = cosmosClient.GetContainer("database", "product");

                await container.CreateItemAsync(GetCosmicSock);

                logger.LogInformation("Added Cosmic Sock to product");

            }
            catch (CosmosException ex)
            {
                //Ignore conflict errors.
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    logger.LogInformation("Product Already added.");
                }
                else
                {
                    logger.LogError(ex.Message);
                    throw;
                }

            }

        }

        public async Task RemoveProduct(CosmosClient cosmosClient, ILogger logger)
        {

            try
            {

                Container embeddingContainer = cosmosClient.GetContainer("database", "embedding");
                Container productContainer = cosmosClient.GetContainer("database", "product");


                try 
                { 

                    //Delete from Cosmos product container
                    await productContainer.DeleteItemAsync<Product>(id: GetCosmicSock.id, partitionKey: new PartitionKey(GetCosmicSock.categoryId));

                }
                catch (CosmosException ex) 
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        logger.LogInformation("Cosmic Sock alread removed from product");
                    }
                    else
                        throw;
                
                }

                //Lookup DocumentVector Id
                string documentVectorId = await LookupDocumentVectorId(embeddingContainer, "product", GetCosmicSock.categoryId, GetCosmicSock.id);
                
                //if this is empty then already gone from Redis and embedding container
                if(!string.IsNullOrEmpty(documentVectorId))
                {
                    //Remove from Redis
                    await _redis.RemoveVector(documentVectorId);
                    //Then delete from embedding container
                    await embeddingContainer.DeleteItemAsync<DocumentVector>(id: documentVectorId, partitionKey: new PartitionKey(documentVectorId));

                    logger.LogInformation("Removed Cosmic Sock from product");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw;

            }
        }

        public Product GetCosmicSock
        {
            get => new Product(
                id: "00001",
                categoryId: "C48B4EF4-D352-4CD2-BCB8-CE89B7DFA642",
                categoryName: "Clothing, Socks",
                sku: "SO-R999-M",
                name: "Cosmic Racing Socks, M",
                description: "The product called Cosmic Racing Socks, M",
                price: 6.00,
                tags: new List<Tag>
                {
                    new Tag(id: "51CD93BF-098C-4C25-9829-4AD42046D038", name: "Tag-25"),
                    new Tag(id: "5D24B427-1402-49DE-B79B-5A7013579FBC", name: "Tag-76"),
                    new Tag(id: "D4EC9C09-75F3-4ADD-A6EB-ACDD12C648FA", name: "Tag-153")
                });
        }

        public async Task<string> LookupDocumentVectorId(Container embeddingContainer, string containerName, string partitionKey, string itemId)
        {

            string documentVectorId = string.Empty;

            using var feedIterator = embeddingContainer.GetItemQueryIterator<DocumentVector>(
                new QueryDefinition("SELECT * FROM c WHERE c.containerName = @containerName AND c.partitionKey = @partitionKey AND c.itemId = @itemId")
                .WithParameter("@containerName", containerName)
                .WithParameter("@partitionKey", partitionKey)
                .WithParameter("@itemId", itemId));


            while (feedIterator.HasMoreResults)
            {

                var response = await feedIterator.ReadNextAsync();

                foreach (var item in response)
                {
                    documentVectorId = item.id;
                }
            }

            return documentVectorId;
        }
    }
}
