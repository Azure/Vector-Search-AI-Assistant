using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Vectorize.Models;
using System.Runtime.CompilerServices;

namespace Vectorize.Services
{
    public class MongoDbService
    {
        private readonly MongoClient? _client;
        private readonly IMongoDatabase? _database;
        private readonly string? _collectionName;
        private readonly IMongoCollection<BsonDocument>? _collection;
        private readonly ILogger<MongoDbService>? _logger;

        public MongoDbService(string connection, string databaseName, string collectionName, ILogger<MongoDbService> logger)
        {


            try
            { 
                _client = new MongoClient(connection);
                _database = _client.GetDatabase(databaseName);
                _collectionName = collectionName;
                _collection = _database.GetCollection<BsonDocument>(_collectionName);
                _logger = logger;

                string vectorIndexName = "vectorSearchIndex";

                //Find if vector index exists
                using (IAsyncCursor<BsonDocument> indexCursor = _collection.Indexes.List())
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
                _logger.LogError("MongoDbService Init failure: " + ex.Message);
            }
        }

        
        public async Task InsertVector(BsonDocument document, ILogger logger)
        {

            if (!document.Contains("_id"))
            {
                logger.LogError("Document does not contain _id.");
                throw new ArgumentException("Document does not contain _id.");
            }

            string? _idValue = document.GetValue("_id").ToString();

            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", _idValue);
                var options = new ReplaceOptions { IsUpsert = true };
                await _collection.ReplaceOneAsync(filter, document, options);

                logger.LogInformation("Inserted new vector into MongoDB");
            }
            catch (Exception ex)
            {
                //TODO: fix the logger. Output does not show up anywhere
                logger.LogError(ex.Message);
                throw;
            }

        }

        public async Task DeleteVector(string categoryId, string id, ILogger logger)
        {

            try
            {

                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("categoryId", categoryId),
                    Builders<BsonDocument>.Filter.Eq("_id", id));

                await _collection.DeleteOneAsync(filter);

            }
            catch (Exception ex) 
            {
                logger.LogError(ex.Message);
                throw;

            }
        }



    }
}
