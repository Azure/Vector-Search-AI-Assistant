namespace Search.Services
{
    using MongoDB.Bson;
    using MongoDB.Driver;
    using Search.Models;
    using ZstdSharp.Unsafe;

    /// <summary>
    /// Service to access Azure Cosmos DB for Mongo vCore.
    /// </summary>
    public class MongoDbService
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        
        private readonly IMongoCollection<Product> _products;
        private readonly IMongoCollection<Customer> _customers;
        private readonly IMongoCollection<BsonDocument> _vectors;
        private readonly IMongoCollection<Session> _sessions;
        private readonly IMongoCollection<Message> _messages;

        private readonly int _maxVectorSearchResults = default;

        private readonly OpenAiService _openAiService;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of the service.
        /// </summary>
        /// <param name="endpoint">Endpoint URI.</param>
        /// <param name="key">Account key.</param>
        /// <param name="databaseName">Name of the database to access.</param>
        /// <param name="collectionNames">Names of the collections for this retail sample.</param>
        /// <exception cref="ArgumentNullException">Thrown when endpoint, key, databaseName, or collectionNames is either null or empty.</exception>
        /// <remarks>
        /// This constructor will validate credentials and create a service client instance.
        /// </remarks>
        public MongoDbService(string connection, string databaseName, string collectionNames, string maxVectorSearchResults, OpenAiService openAiService, ILogger logger)
        {
            ArgumentException.ThrowIfNullOrEmpty(connection);
            ArgumentException.ThrowIfNullOrEmpty(databaseName);
            ArgumentException.ThrowIfNullOrEmpty(collectionNames);
            ArgumentException.ThrowIfNullOrEmpty(maxVectorSearchResults);
            

            _openAiService = openAiService;
            _logger = logger;

            _client = new MongoClient(connection);
            _database = _client.GetDatabase(databaseName);
            _maxVectorSearchResults = int.TryParse(maxVectorSearchResults, out _maxVectorSearchResults) ? _maxVectorSearchResults : 10;

            //product, customer, vectors, completions  //Not used
            List<string> collections = collectionNames.Split(',').ToList();

            _products = _database.GetCollection<Product>("product");
            _customers = _database.GetCollection<Customer>("customer");
            _vectors = _database.GetCollection<BsonDocument>("vectors");
            _sessions = _database.GetCollection<Session>("completions");
            _messages = _database.GetCollection<Message>("completions");

            CreateVectorIndexIfNotExists(_vectors);
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
            catch (MongoException ex)
            {
                _logger.LogError("MongoDbService InitializeVectorIndex: " + ex.Message);
                throw;
            }

        }

        public async Task<string> VectorSearchAsync(float[] embeddings)
        {
            List<string> retDocs = new List<string>();

            string resultDocuments = string.Empty;

            try
            {
                //Search Mongo vCore collection for similar embeddings
                //Project the fields that are needed
                BsonDocument[] pipeline = new BsonDocument[]
                {
                    BsonDocument.Parse($"{{$search: {{cosmosSearch: {{ vector: [{string.Join(',', embeddings)}], path: 'vector', k: {_maxVectorSearchResults}}}, returnStoredSource:true}}}}"),
                    BsonDocument.Parse($"{{$project: {{_id: 0, vector: 0}}}}"),
                };

                // Return results, combine into a single string
                List<BsonDocument> bsonDocuments = await _vectors.Aggregate<BsonDocument>(pipeline).ToListAsync();
                List<string> result = bsonDocuments.ConvertAll(bsonDocument => bsonDocument.ToString());
                resultDocuments = (string.Join(" ", result));

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: VectorSearchAsync(): {ex.Message}");
                throw;
            }

            return resultDocuments;
        }

        /// <summary>
        /// Gets a list of all current chat sessions.
        /// </summary>
        /// <returns>List of distinct chat session items.</returns>
        public async Task<List<Session>> GetSessionsAsync()
        {
            List<Session> sessions = new List<Session>();
            try
            {

                sessions = await _sessions.Find(
                    filter: Builders<Session>.Filter.Eq("Type", nameof(Session)))
                    .ToListAsync();

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: GetSessionsAsync(): {ex.Message}");
                throw;
            }

            return sessions;
        }

        /// <summary>
        /// Gets a list of all current chat messages for a specified session identifier.
        /// </summary>
        /// <param name="sessionId">Chat session identifier used to filter messsages.</param>
        /// <returns>List of chat message items for the specified session.</returns>
        public async Task<List<Message>> GetSessionMessagesAsync(string sessionId)
        {
            List<Message> messages = new();

            try
            {
                
                messages = await _messages.Find(
                    filter: Builders<Message>.Filter.Eq("Type", nameof(Message))
                    & Builders<Message>.Filter.Eq("SessionId", sessionId))
                    .ToListAsync();
                
            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: GetSessionMessagesAsync(): {ex.Message}");
                throw;
            }

            return messages;

        }

        /// <summary>
        /// Creates a new chat session.
        /// </summary>
        /// <param name="session">Chat session item to create.</param>
        /// <returns>Newly created chat session item.</returns>
        public async Task InsertSessionAsync(Session session)
        {
            try
            {
                
                await _sessions.InsertOneAsync(session);

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: InsertSessionAsync(): {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new chat message.
        /// </summary>
        /// <param name="message">Chat message item to create.</param>
        /// <returns>Newly created chat message item.</returns>
        public async Task InsertMessageAsync(Message message)
        {
            try
            {
                
                await _messages.InsertOneAsync(message);

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: InsertMessageAsync(): {ex.Message}");
                throw;
            }

        }

        /// <summary>
        /// Updates an existing chat session.
        /// </summary>
        /// <param name="session">Chat session item to update.</param>
        /// <returns>Revised created chat session item.</returns>
        public async Task UpdateSessionAsync(Session session)
        {

            try
            {

                await _sessions.ReplaceOneAsync(
                    filter: Builders<Session>.Filter.Eq("Type", nameof(Session))
                    & Builders<Session>.Filter.Eq("SessionId", session.SessionId),
                    replacement: session);

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: UpdateSessionAsync(): {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Batch create or update chat messages and session.
        /// </summary>
        /// <param name="messages">Chat message and session items to create or replace.</param>
        public async Task UpsertSessionBatchAsync(Session session, Message promptMessage, Message completionMessage)
        {
            using (var transasction = await _client.StartSessionAsync())
            {
                transasction.StartTransaction();

                try
                {
                    
                    await _sessions.ReplaceOneAsync(
                        filter: Builders<Session>.Filter.Eq("Type", nameof(Session))
                            & Builders<Session>.Filter.Eq("SessionId", session.SessionId)
                            & Builders<Session>.Filter.Eq("Id", session.Id),
                        replacement: session);

                    await _messages.InsertOneAsync(promptMessage);
                    await _messages.InsertOneAsync(completionMessage);

                    await transasction.CommitTransactionAsync();
                }
                catch (MongoException ex)
                {
                    await transasction.AbortTransactionAsync();
                    _logger.LogError($"Exception: UpsertSessionBatchAsync(): {ex.Message}");
                    throw;
                }
            }


        }

        /// <summary>
        /// Batch deletes an existing chat session and all related messages.
        /// </summary>
        /// <param name="sessionId">Chat session identifier used to flag messages and sessions for deletion.</param>
        public async Task DeleteSessionAndMessagesAsync(string sessionId)
        {
            try
            {

                await _database.GetCollection<BsonDocument>("completions").DeleteManyAsync(
                    filter: Builders<BsonDocument>.Filter.Eq("SessionId", sessionId));

            }
            catch (MongoException ex)
            {
                _logger.LogError($"Exception: DeleteSessionAndMessagesAsync(): {ex.Message}");
                throw;
            }

        }

    }
}