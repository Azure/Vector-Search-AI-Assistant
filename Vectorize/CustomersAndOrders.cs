using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Search;

namespace Vectorize
{
    public class CustomersAndOrders
    {

        private readonly IOpenAiService _openAiService;
        private readonly ICognitiveSearchServiceManagement _cognitiveSearchService;
        private readonly ILogger _logger;

        public CustomersAndOrders(IOpenAiService openAiService, ICognitiveSearchServiceManagement cognitiveSearchService,
            ILoggerFactory loggerFactory)
        {
            _openAiService = openAiService;
            _cognitiveSearchService = cognitiveSearchService;
            _logger = loggerFactory.CreateLogger<CustomersAndOrders>();
        }

        [Function("CustomersAndOrders")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "database",
                containerName: "customer",
                StartFromBeginning = true,
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<JObject> input)
        {

            if (input != null && input.Count > 0)
            {

                _logger.LogInformation("Generating embeddings for " + input.Count + " Customers and Sales Orders");

                // Using dynamic type as this container has two different entities
                foreach (dynamic item in input)
                {
                    if (item.type == "customer")
                    {
                        Customer customer = item.ToObject<Customer>();
                        await GenerateCustomerVectors(customer);
                    }

                    else if (item.type == "salesOrder")
                    {
                        SalesOrder salesOrder = item.ToObject<SalesOrder>();
                        await GenerateSalesOrderVectors(salesOrder);

                    }
                    else
                    {
                        _logger.LogError($"Unsupported entity saved in customer container: {item.type}");
                    }
                }
            }
        }

        public async Task GenerateCustomerVectors(Customer customer)
        {
            // Serialize the object to send to OpenAI
            var sDocument = JObject.FromObject(customer).ToString();

            try
            {
                // Get the embeddings from OpenAI
                var (vector, responseTokens) = await _openAiService.GetEmbeddingsAsync(sDocument);
                
                customer.vector = vector;

                // Save to Cognitive Search
                await _cognitiveSearchService.InsertVector(customer);

                _logger.LogInformation($"Saved vector for customer: {customer.firstName} {customer.lastName} ");

            }
            catch (Exception x)
            {
                _logger.LogError($"Exception while generating vector for customer: {customer.firstName} {customer.lastName}" + x.Message);
            }

        }

        public async Task GenerateSalesOrderVectors(SalesOrder salesOrder)
        {
            // Serialize the object to send to OpenAI
            var sDocument = JObject.FromObject(salesOrder).ToString();

            try
            {
                // Get the embeddings from OpenAI
                var(vector, responseTokens) = await _openAiService.GetEmbeddingsAsync(sDocument);
                salesOrder.vector = vector;

                // Save to Cognitive Search
                await _cognitiveSearchService.InsertVector(salesOrder);

                _logger.LogInformation($"Saved vector for sales order id: {salesOrder.id} and customer id: {salesOrder.customerId} ");

            }
            catch (Exception x)
            {
                _logger.LogError($"Exception while generating vector for sales order id: {salesOrder.id} and customer id: {salesOrder.customerId}" + x.Message);
            }

        }
    }
}
