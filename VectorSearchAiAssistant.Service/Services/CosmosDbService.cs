using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using VectorSearchAiAssistant.Service.Models.Chat;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Search;

namespace VectorSearchAiAssistant.Service.Services
{
    /// <summary>
    /// Service to access Azure Cosmos DB for NoSQL.
    /// </summary>
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _completions;
        private readonly Container _customer;
        private readonly Container _product;
        private readonly Database _database;
        private readonly Dictionary<string, Container> _containers;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of the service.
        /// </summary>
        /// <param name="endpoint">Endpoint URI.</param>
        /// <param name="key">Account key.</param>
        /// <param name="databaseName">Name of the database to access.</param>
        /// <param name="containerNames">Names of the containers to access.</param>
        /// <exception cref="ArgumentNullException">Thrown when endpoint, key, databaseName, or containerNames is either null or empty.</exception>
        /// <remarks>
        /// This constructor will validate credentials and create a service client instance.
        /// </remarks>
        public CosmosDbService(string endpoint, string key, string databaseName, string containerNames, ILogger logger)
        {
            ArgumentException.ThrowIfNullOrEmpty(endpoint);
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentException.ThrowIfNullOrEmpty(databaseName);
            ArgumentException.ThrowIfNullOrEmpty(containerNames);

            _logger = logger;

            CosmosSerializationOptions options = new()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            };

            CosmosClient client = new CosmosClientBuilder(endpoint, key)
                .WithSerializerOptions(options)
                .Build();

            Database? database = client?.GetDatabase(databaseName);

            _database = database ??
                        throw new ArgumentException("Unable to connect to existing Azure Cosmos DB database.");


            //Dictionary of container references for all containers listed in config
            _containers = new Dictionary<string, Container>();

            List<string> containers = containerNames.Split(',').ToList();

            foreach (string containerName in containers)
            {
                Container? container = database?.GetContainer(containerName.Trim()) ??
                                       throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");

                _containers.Add(containerName.Trim(), container);
            }

            _completions = _containers["completions"];
            _customer = _containers["customer"];
            _product = _containers["product"];
        }

        /// <summary>
        /// Gets a list of all current chat sessions.
        /// </summary>
        /// <returns>List of distinct chat session items.</returns>
        public async Task<List<Session>> GetSessionsAsync()
        {
            QueryDefinition query = new QueryDefinition("SELECT DISTINCT * FROM c WHERE c.type = @type")
                .WithParameter("@type", nameof(Session));

            FeedIterator<Session> response = _completions.GetItemQueryIterator<Session>(query);

            List<Session> output = new();
            while (response.HasMoreResults)
            {
                FeedResponse<Session> results = await response.ReadNextAsync();
                output.AddRange(results);
            }

            return output;
        }

        /// <summary>
        /// Gets a list of all current chat messages for a specified session identifier.
        /// </summary>
        /// <param name="sessionId">Chat session identifier used to filter messsages.</param>
        /// <returns>List of chat message items for the specified session.</returns>
        public async Task<List<Message>> GetSessionMessagesAsync(string sessionId)
        {
            QueryDefinition query =
                new QueryDefinition("SELECT * FROM c WHERE c.sessionId = @sessionId AND c.type = @type")
                    .WithParameter("@sessionId", sessionId)
                    .WithParameter("@type", nameof(Message));

            FeedIterator<Message> results = _completions.GetItemQueryIterator<Message>(query);

            List<Message> output = new();
            while (results.HasMoreResults)
            {
                FeedResponse<Message> response = await results.ReadNextAsync();
                output.AddRange(response);
            }

            return output;
        }

        /// <summary>
        /// Creates a new chat session.
        /// </summary>
        /// <param name="session">Chat session item to create.</param>
        /// <returns>Newly created chat session item.</returns>
        public async Task<Session> InsertSessionAsync(Session session)
        {
            PartitionKey partitionKey = new(session.SessionId);
            return await _completions.CreateItemAsync(
                item: session,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Creates a new chat message.
        /// </summary>
        /// <param name="message">Chat message item to create.</param>
        /// <returns>Newly created chat message item.</returns>
        public async Task<Message> InsertMessageAsync(Message message)
        {
            PartitionKey partitionKey = new(message.SessionId);
            return await _completions.CreateItemAsync(
                item: message,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Updates an existing chat message.
        /// </summary>
        /// <param name="message">Chat message item to update.</param>
        /// <returns>Revised chat message item.</returns>
        public async Task<Message> UpdateMessageAsync(Message message)
        {
            PartitionKey partitionKey = new(message.SessionId);
            return await _completions.ReplaceItemAsync(
                item: message,
                id: message.Id,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Updates an existing chat session.
        /// </summary>
        /// <param name="session">Chat session item to update.</param>
        /// <returns>Revised created chat session item.</returns>
        public async Task<Session> UpdateSessionAsync(Session session)
        {
            PartitionKey partitionKey = new(session.SessionId);
            return await _completions.ReplaceItemAsync(
                item: session,
                id: session.Id,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Batch create or update chat messages and session.
        /// </summary>
        /// <param name="messages">Chat message and session items to create or replace.</param>
        public async Task UpsertSessionBatchAsync(params dynamic[] messages)
        {
            if (messages.Select(m => m.SessionId).Distinct().Count() > 1)
            {
                throw new ArgumentException("All items must have the same partition key.");
            }

            PartitionKey partitionKey = new(messages.First().SessionId);
            TransactionalBatch batch = _completions.CreateTransactionalBatch(partitionKey);
            foreach (var message in messages)
            {
                batch.UpsertItem(
                    item: message
                );
            }

            await batch.ExecuteAsync();
        }

        /// <summary>
        /// Batch deletes an existing chat session and all related messages.
        /// </summary>
        /// <param name="sessionId">Chat session identifier used to flag messages and sessions for deletion.</param>
        public async Task DeleteSessionAndMessagesAsync(string sessionId)
        {
            PartitionKey partitionKey = new(sessionId);

            // TODO: await container.DeleteAllItemsByPartitionKeyStreamAsync(partitionKey);

            QueryDefinition query = new QueryDefinition("SELECT c.id FROM c WHERE c.sessionId = @sessionId")
                .WithParameter("@sessionId", sessionId);

            FeedIterator<Message> response = _completions.GetItemQueryIterator<Message>(query);

            TransactionalBatch batch = _completions.CreateTransactionalBatch(partitionKey);
            while (response.HasMoreResults)
            {
                FeedResponse<Message> results = await response.ReadNextAsync();
                foreach (var item in results)
                {
                    batch.DeleteItem(
                        id: item.Id
                    );
                }
            }

            await batch.ExecuteAsync();
        }

        /// <summary>
        /// Inserts a product into the product container.
        /// </summary>
        /// <param name="product">Product item to create.</param>
        /// <returns>Newly created product item.</returns>
        public async Task<Product> InsertProductAsync(Product product)
        {
            try
            {
                return await _product.CreateItemAsync(product);
            }
            catch (CosmosException ex)
            {
                // Ignore conflict errors.
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    _logger.LogInformation("Product Already added.");
                }
                else
                {
                    _logger.LogError(ex.Message);
                    throw;
                }
                return product;
            }
        }

        /// <summary>
        /// Deletes a product by its Id and category (its partition key).
        /// </summary>
        /// <param name="productId">The Id of the product to delete.</param>
        /// <param name="categoryId">The category Id of the product to delete.</param>
        /// <returns></returns>
        public async Task DeleteProduct(string productId, string categoryId)
        {
            try
            {
                // Delete from Cosmos product container
                await _product.DeleteItemAsync<Product>(id: productId, partitionKey: new PartitionKey(categoryId));
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("The product has already been removed.");
                }
                else
                    throw;
            }
        }

        /// <summary>
        /// Reads all documents retrieved by Vector Search.
        /// </summary>
        /// <param name="vectorDocuments">List string of JSON documents from vector search results</param>
        public async Task<string> GetVectorSearchDocumentsAsync(List<DocumentVector> vectorDocuments)
        {

            List<string> searchDocuments = new List<string>();

            foreach (var document in vectorDocuments)
            {

                try
                {
                    var response = await _containers[document.containerName].ReadItemStreamAsync(
                        document.itemId, new PartitionKey(document.partitionKey));


                    if ((int) response.StatusCode < 200 || (int) response.StatusCode >= 400)
                        _logger.LogError(
                            $"Failed to retrieve an item for id '{document.itemId}' - status code '{response.StatusCode}");

                    if (response.Content == null)
                    {
                        _logger.LogInformation(
                            $"Null content received for document '{document.itemId}' - status code '{response.StatusCode}");
                        continue;
                    }

                    string item;
                    using (StreamReader sr = new StreamReader(response.Content))
                        item = await sr.ReadToEndAsync();

                    searchDocuments.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);

                }
            }

            var resultDocuments = string.Join(Environment.NewLine + "-", searchDocuments);

            return resultDocuments;

        }
    }
}
