using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Vectorize.Models;
using Newtonsoft.Json.Linq;

namespace Vectorize.Services
{
    public class MongoDbService
    {
        private readonly MongoClient? _client;
        private readonly IMongoDatabase? _database;
        private readonly Dictionary<string, IMongoCollection<BsonDocument>> _collections;

        private readonly OpenAiService _openAiService;
        private readonly ILogger _logger;

        public MongoDbService(string connection, string databaseName, string collectionNames, OpenAiService openAiService, ILogger logger)
        {

            _logger = logger;
            _openAiService = openAiService;
            
            _collections = new Dictionary<string, IMongoCollection<BsonDocument>>();

            try
            {
                _client = new MongoClient(connection);
                _database = _client.GetDatabase(databaseName);

                //product, customer, vectors, completions
                List<string> collections = collectionNames.Split(',').ToList();


                foreach (string collectionName in collections)
                {

                    IMongoCollection<BsonDocument>? collection = _database.GetCollection<BsonDocument>(collectionName.Trim()) ??
                        throw new ArgumentException("Unable to connect to existing Azure Cosmos DB for MongoDB vCore collection or database.");

                    _collections.Add(collectionName, collection);
                }

                CreateVectorIndexIfNotExists(_collections["vectors"]);


            }
            catch (Exception ex)
            {
                _logger.LogError("MongoDbService Init failure: " + ex.Message);
            }
        }

        public void CreateVectorIndexIfNotExists(IMongoCollection<BsonDocument> vectorCollection)
        {

            try
            {
                string vectorIndexName = "vectorSearchIndex";

                //Find if vector index exists in vectors collection
                using (IAsyncCursor<BsonDocument> indexCursor = vectorCollection.Indexes.List())
                {
                    bool vectorIndexExists = indexCursor.ToList().Any(x => x["name"] == vectorIndexName);
                    if (!vectorIndexExists)
                    {
                        BsonDocumentCommand<BsonDocument> command = new BsonDocumentCommand<BsonDocument>(
                        BsonDocument.Parse(@"
                            { createIndexes: 'vectors', 
                              indexes: [{ 
                                name: 'vectorSearchIndex', 
                                key: { vector: 'cosmosSearch' }, 
                                cosmosSearchOptions: { kind: 'vector-ivf', numLists: 5, similarity: 'COS', dimensions: 1536 } 
                              }] 
                            }"));

                        BsonDocument result = _database.RunCommand(command);
                        if (result["ok"] != 1)
                        {
                            _logger.LogError("CreateIndex failed with response: " + result.ToJson());
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("MongoDbService InitializeVectorIndex: " + ex.Message);
            }

        }

        
        public async Task UpsertVectorAsync(BsonDocument document)
        {

            //Since we store all the vectors in the same collection just need one function to handle everything

            if (!document.Contains("_id"))
            {
                _logger.LogError("UpsertVectorAsync: Document does not contain _id.");
                throw new ArgumentException("UpsertVectorAsync: Document does not contain _id.");
            }

            string? _idValue = document["_id"].ToString();

            
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", _idValue);
                var options = new ReplaceOptions { IsUpsert = true };
                await _collections["vectors"].ReplaceOneAsync(filter, document, options);

            }
            catch (Exception ex)
            {
                
                _logger.LogError($"Exception: UpsertVectorAsync(): {ex.Message}");
                throw;
            }

        }


        public async Task<Product> UpsertProductAsync(Product product)
        {

            //Add to product collection first, then vectorize and store in vectors collection.
            //You could store the vectors in product collection but it is simpler to store
            //in a single collection and search there.

            try
            {

                var bsonItem = product.ToBsonDocument();

                await _collections["product"].ReplaceOneAsync(
                    filter: Builders<BsonDocument>.Filter.Eq("categoryId", product.categoryId)
                          & Builders<BsonDocument>.Filter.Eq("_id", product.id),
                    options: new ReplaceOptions { IsUpsert = true },
                    replacement: bsonItem);


                //TO DO: Make this simpler

                //Store in vectors collection
                //Serialize the product object to send to OpenAI
                string sProduct = JObject.FromObject(product).ToString();
                product.vector = await _openAiService.GetEmbeddingsAsync(sProduct);
                await UpsertVectorAsync(product.ToBsonDocument());

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: UpsertProductAsync(): {ex.Message}");
                throw;

            }

            return product;
        }

        public async Task DeleteProductAsync(Product product)
        {

            try
            {
                
                var filter = Builders<BsonDocument>.Filter.And(
                     Builders<BsonDocument>.Filter.Eq("categoryId", product.categoryId),
                     Builders<BsonDocument>.Filter.Eq("_id", product.id));

                //Delete from the product collection
                await _collections["customer"].DeleteOneAsync(filter);

                //Delete from vectors collection
                await _collections["vectors"].DeleteOneAsync(filter);

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: DeleteProductAsync(): {ex.Message}");
                throw;

            }

        }

        public async Task<Customer> UpsertCustomerAsync(Customer customer)
        {

            //Add to customer collection first, then vectorize and store in vectors collection.
            //You could store the vectors in customer collection but it is simpler to store
            //in a single collection and search there.

            try
            {
                var bsonItem = customer.ToBsonDocument();

                await _collections["customer"].ReplaceOneAsync(
                    filter: Builders<BsonDocument>.Filter.Eq("customerId", customer.customerId)
                          & Builders<BsonDocument>.Filter.Eq("_id", customer.id),
                    options: new ReplaceOptions { IsUpsert = true },
                    replacement: bsonItem);

                //TO DO: Make this simpler

                //Store in vectors collection
                //Serialize the customer object to send to OpenAI
                string sCustomer = JObject.FromObject(customer).ToString();
                customer.vector = await _openAiService.GetEmbeddingsAsync(sCustomer);
                await UpsertVectorAsync(customer.ToBsonDocument());


            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: UpsertCustomerAsync(): {ex.Message}");
                throw;

            }

            return customer;

        }

        public async Task DeleteCustomerAsync(Customer customer)
        {

            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("customerId", customer.customerId)
                            & Builders<BsonDocument>.Filter.Eq("_id", customer.id);

                await _collections["customer"].DeleteOneAsync(filter);

                //Note: This sample does not add/remove a customer object so vector deletion not implemented

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: DeleteCustomerAsync(): {ex.Message}");
                throw;

            }

        }

        public async Task<SalesOrder> UpsertSalesOrderAsync(SalesOrder salesOrder)
        {

            //Add to customer collection first, then vectorize and store in vectors collection.
            //You could store the vectors in customer collection but it is simpler to store
            //in a single collection and search there.

            try
            {
                var bsonItem = salesOrder.ToBsonDocument();

                await _collections["customer"].ReplaceOneAsync(
                    filter: Builders<BsonDocument>.Filter.Eq("customerId", salesOrder.customerId)
                          & Builders<BsonDocument>.Filter.Eq("_id", salesOrder.id),
                    options: new ReplaceOptions { IsUpsert = true },
                    replacement: bsonItem);

                //TO DO: Make this simpler

                //Store in vectors collection
                //Serialize the customer object to send to OpenAI
                string sOrder = JObject.FromObject(salesOrder).ToString();
                salesOrder.vector = await _openAiService.GetEmbeddingsAsync(sOrder);
                await UpsertVectorAsync(salesOrder.ToBsonDocument());

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: UpsertSalesOrderAsync(): {ex.Message}");
                throw;

            }

            return salesOrder;

        }

        public async Task DeleteSalesOrderAsync(SalesOrder salesOrder)
        {

            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("customerId", salesOrder.customerId)
                            & Builders<BsonDocument>.Filter.Eq("_id", salesOrder.id);

                await _collections["customer"].DeleteOneAsync(filter);

                //Note: This sample does not add/remove a customer object so vector deletion not implemented

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: DeleteSalesOrderAsync(): {ex.Message}");
                throw;

            }

        }

        public async Task<int> InitialProductVectorsAsync()
        {
            try
            {

                _logger.LogInformation("Generating vectors for Products");

                
                var filter = new BsonDocument();
                int productCount = 1;


                using (var cursor = await _collections["product"].Find(filter).ToCursorAsync())
                {
                    while (await cursor.MoveNextAsync())
                    {
                        var batch = cursor.Current;

                        foreach(var document in batch)
                        { 

                            //Deserialize to Product to accept the vector data
                            Product product = BsonSerializer.Deserialize<Product>(document);

                            //Generate vectors
                            product.vector = await _openAiService.GetEmbeddingsAsync(document.ToString());

                            //Save to vector collection
                            await UpsertVectorAsync(product.ToBsonDocument());

                            productCount++;
                            if (productCount %100 == 0)
                                _logger.LogInformation($"Generated {productCount} product vectors");
                        }
                    }
                }

                _logger.LogInformation($"Product vector generation complete. {productCount} vectors generated");

                return productCount;
            }
            catch(MongoException ex)
            {
                _logger.LogError($"Exception: InitialProductVectorsAsync(): {ex.Message}");
                throw;
            }
        }

        public async Task<(int customerVectors, int orderVectors)> InitialCustomerAndSalesOrderVectorsAsync()
        {
            try
            {
                _logger.LogInformation("Generating vectors for Customers and Sales Orders");

                
                var filter = new BsonDocument();
                int orderCount = 1;
                int customerCount = 1;


                using (var cursor = await _collections["customer"].Find(filter).ToCursorAsync())
                {
                    while (await cursor.MoveNextAsync())
                    {
                        var batch = cursor.Current;

                        foreach (var document in batch)
                        {

                            if (document["type"] == "salesOrder")
                            {
                                //Deserialize to accept the vector data
                                SalesOrder salesOrder = BsonSerializer.Deserialize<SalesOrder>(document);

                                //Generate vectors
                                salesOrder.vector = await _openAiService.GetEmbeddingsAsync(document.ToString());

                                //Save to vector collection
                                await UpsertVectorAsync(salesOrder.ToBsonDocument());

                                orderCount++;
                                if (orderCount % 100 == 0)
                                    _logger.LogInformation($"Generated {orderCount} sales order vectors");

                            }
                            else if (document["type"] == "customer")
                            {
                                //Deserialize to accept the vector data
                                Customer customer = BsonSerializer.Deserialize<Customer>(document);

                                //Generate vectors
                                customer.vector = await _openAiService.GetEmbeddingsAsync(document.ToString());

                                //Save to vector collection
                                await UpsertVectorAsync(customer.ToBsonDocument());

                                customerCount++;
                                if (customerCount % 100 == 0)
                                    _logger.LogInformation($"Generated {customerCount} customer vectors");
                            }
                            else
                            {
                                _logger.LogError($"Exception: InitialCustomerAndSalesOrderVectorsAsync(): Unrecognized type in customer container");

                            }
                        }
                    }
                }

                _logger.LogInformation($"Customer and Sales Order vector generation complete");

                return (customerVectors: customerCount, orderVectors: orderCount);
            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: InitialCustomerAndSalesOrderVectorsAsync(): {ex.Message}");
                throw;
            }
        }

        public async Task ImportJsonAsync(string collectionName, string json)
        {
            try
            {

                IMongoCollection<BsonDocument> collection = _collections[collectionName];
                var documents = BsonSerializer.Deserialize<IEnumerable<BsonDocument>>(json);
                await collection.InsertManyAsync(documents);
            }

            catch (MongoException ex)
            {
                _logger.LogError($"Exception: ImportJsonAsync(): {ex.Message}");
                throw;
            }
        }

    }
}
