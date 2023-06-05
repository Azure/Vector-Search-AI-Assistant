using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using VectorSearchAiAssistant.Service.Models.Search;
using VectorSearchAiAssistant.Service.Interfaces;
using System.Net;

namespace Vectorize
{
    public class AddRemoveData
    {

        private readonly ICognitiveSearchServiceManagement _cognitiveSearchService;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly ILogger _logger;

        public AddRemoveData(ICognitiveSearchServiceManagement cognitiveSearchService, ICosmosDbService cosmosDbService,
            ILoggerFactory loggerFactory)
        {
            _cognitiveSearchService = cognitiveSearchService;
            _cosmosDbService = cosmosDbService;
            _logger = loggerFactory.CreateLogger<AddRemoveData>();
        }


        [Function("AddRemoveData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string? action = req.Query["action"];

            try
            { 

                if (action == "add")
                {
                    await AddProduct();
                }
                else if (action == "remove")
                {
                    await RemoveProduct();

                }
                else
                {
                    throw new Exception("Bad Request: Missing value for action in query string, add or remove");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                await response.WriteStringAsync("HTTP trigger function executed successfully.");

                return response;
            }
            catch (Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync(ex.ToString());
                return response;

            }
        }

        public async Task AddProduct() 
        {

            await _cosmosDbService.InsertProductAsync(GetCosmicSock);

            _logger.LogInformation("Added Cosmic Sock to product");
        }

        public async Task RemoveProduct()
        {
            await _cosmosDbService.DeleteProduct(GetCosmicSock.id, GetCosmicSock.categoryId);

            try
            {
                    //just ignore any error
                    await _cognitiveSearchService.DeleteVector(GetCosmicSock);

                    _logger.LogInformation("Removed Cosmic Sock from product");

            }
            catch(Exception ex) 
            { 
                _logger.LogError(ex.Message);
                //throw;
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
