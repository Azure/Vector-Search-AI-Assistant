using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using Vectorize.Models;
using Vectorize.Services;

namespace Vectorize
{
    public class AddRemoveData
    {

        private readonly MongoDbService _mongo;

        public AddRemoveData(MongoDbService mongo)
        {
            _mongo = mongo;
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
            catch(Exception ex) 
            {
                logger.LogError(ex.Message);
                throw;
            
            }

        }

        public async Task RemoveProduct(CosmosClient cosmosClient, ILogger logger)
        {

            try
            {
            
                Container container = cosmosClient.GetContainer("database", "product");

                await container.DeleteItemAsync<Product>(id: GetCosmicSock.id, partitionKey: new PartitionKey(GetCosmicSock.categoryId));

                await _mongo.DeleteVector(GetCosmicSock.categoryId, GetCosmicSock.id, logger);

                logger.LogInformation("Removed Cosmic Sock from product");

            }
            catch(Exception ex) 
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
    }
}
