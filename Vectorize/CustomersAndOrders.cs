using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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

                        
                    await GenerateVectors(item, logger);

                }
                
            }
        }

        public async Task GenerateVectors(dynamic document, ILogger logger)
        {
            //Serialize the object to send to OpenAI
            string sDocument = JObject.FromObject(document).ToString();


            try
            {
                //Get the embeddings from OpenAI
                document.vector = await _openAi.GetEmbeddingsAsync(sDocument, logger);


                //Save to Mongo
                await _mongo.UpsertVector(document, logger);

                logger.LogInformation($"Saved vector for object: {document.type}, id: {document.id} ");

            }
            catch (Exception x)
            {
                logger.LogError($"Exception while generating vector for object: {document.type}, id: {document.id} " + x.Message);
            }

        }
    }
}
