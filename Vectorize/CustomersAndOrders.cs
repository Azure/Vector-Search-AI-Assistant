using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Vectorize.Models;
using Vectorize.Services;

namespace Vectorize
{
    public class CustomersAndOrders
    {

        private readonly OpenAiService _openAi;
        private readonly MongoDbService _mongo;

        public CustomersAndOrders(OpenAiService openAI, MongoDbService mongo)
        {
            _openAi = openAI;
            _mongo = mongo;
        }

        [FunctionName("CustomersAndOrders")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "database",
                containerName: "customer",
                StartFromBeginning = true,
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<JObject> input,
            ILogger logger)
        {

            if (input != null && input.Count > 0)
            {

                logger.LogInformation("Generating embeddings for " + input.Count + "Customers and Sales Orders");


                //using dynamic types throughout as this container has two different entities

                foreach (dynamic item in input)
                {
                    if (item.type == "customer")
                        await GenerateCustomerVectors(item, logger);
                    else
                        await GenerateSalesOrderVectors(item, logger);

                }
                
            }
        }

        public async Task GenerateCustomerVectors(Customer customer, ILogger logger)
        {
            //Serialize the object to send to OpenAI
            string sDocument = JObject.FromObject(customer).ToString();

            try
            {
                //Get the embeddings from OpenAI
                customer.vector = await _openAi.GetEmbeddingsAsync(sDocument);


                //Save to Mongo
                BsonDocument bsonCustomer = customer.ToBsonDocument();
                await _mongo.InsertVector(bsonCustomer);


                logger.LogInformation($"Saved vector for customer: {customer.firstName} {customer.lastName} ");

            }
            catch (Exception x)
            {
                logger.LogError($"Exception while generating vector for customer: {customer.firstName} {customer.lastName}" + x.Message);
            }

        }

        public async Task GenerateSalesOrderVectors(SalesOrder salesOrder, ILogger logger)
        {
            //Serialize the object to send to OpenAI
            string sDocument = JObject.FromObject(salesOrder).ToString();

            try
            {
                //Get the embeddings from OpenAI
                salesOrder.vector = await _openAi.GetEmbeddingsAsync(sDocument);


                //Save to Mongo
                BsonDocument bsonSalesOrder = salesOrder.ToBsonDocument();
                await _mongo.InsertVector(bsonSalesOrder);


                logger.LogInformation($"Saved vector for sales order id: {salesOrder.id} and customer id: {salesOrder.customerId} ");

            }
            catch (Exception x)
            {
                logger.LogError($"Exception while generating vector for sales order id: {salesOrder.id} and customer id: {salesOrder.customerId}" + x.Message);
            }

        }
    }
}
