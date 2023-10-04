using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Vectorize.Services;


namespace Vectorize
{
    public class IngestAndVectorize
    {

        private readonly MongoDbService _mongo;
        private readonly ILogger _logger;

        public IngestAndVectorize(MongoDbService mongo, ILoggerFactory loggerFactory)
        {
            _mongo = mongo;
            _logger = loggerFactory.CreateLogger<IngestAndVectorize>();
        }

        [Function("IngestAndVectorize")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req)
        {
            _logger.LogInformation("Ingest and Vectorize HTTP trigger function is processing a request.");
            try
            {
                
                // Ingest json data into MongoDB collections
                await IngestDataFromBlobStorageAsync();


                //Generate vectors on the data and store in vectors collection
                await GenerateAndStoreVectorsAsync();


                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await response.WriteStringAsync("Ingest and Vectorize HTTP trigger function executed successfully.");

                return response;
            }
            catch (Exception ex)
            {

                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync(ex.ToString());
                return response;

            }
        }

        public async Task IngestDataFromBlobStorageAsync()
        {
            

            try
            {
                BlobContainerClient blobContainerClient = new BlobContainerClient(new Uri("https://cosmosdbcosmicworks.blob.core.windows.net/cosmic-works-mongo/"));


                //Download and ingest product.json
                _logger.LogInformation("Ingesting product data from blob storage.");


                BlobClient productBlob = blobContainerClient.GetBlobClient("product.json");
                BlobDownloadStreamingResult pResult = await productBlob.DownloadStreamingAsync();

                using(StreamReader pReader = new StreamReader(pResult.Content)) 
                {
                    string productJson = await pReader.ReadToEndAsync();
                    await _mongo.ImportJsonAsync("product", productJson);

                }
                _logger.LogInformation("Product data ingestion complete.");



                //Download and ingest customer.json
                _logger.LogInformation("Ingesting customer and order data from blob storage.");

                BlobClient customerBlob = blobContainerClient.GetBlobClient("customer.json");
                BlobDownloadStreamingResult cResult = await customerBlob.DownloadStreamingAsync();

                using (StreamReader reader = new StreamReader(cResult.Content))
                {
                    string customerJson = await reader.ReadToEndAsync();
                    await _mongo.ImportJsonAsync("customer", customerJson);

                }
                _logger.LogInformation("Customer and order data ingestion complete.");
            }
            catch(Exception ex)
            {
                _logger.LogError($"Exception: IngestDataFromBlobStorageAsync(): {ex.Message}");
                throw;
            }
        }

        public async Task GenerateAndStoreVectorsAsync()
        {

            try
            {
                //Generate Product Vectors and store in vectors collection
                int productVectors = await _mongo.InitialProductVectorsAsync();

                //Generate Customer and Sales Order Vectors and store in vectors collection
                (int customerVectors, int orderVectors) = await _mongo.InitialCustomerAndSalesOrderVectorsAsync();

                _logger.LogInformation("Generate and Store Vectors Complete.");
                _logger.LogInformation($"{productVectors} products completed.");
                _logger.LogInformation($"{customerVectors} customers completed.");
                _logger.LogInformation($"{orderVectors} orders completed.");

            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: GenerateAndStoreVectorsAsync(): {ex.Message}");
                throw;
            }
        }
    }
}
