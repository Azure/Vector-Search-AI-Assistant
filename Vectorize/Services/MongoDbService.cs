using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Vectorize.Models;

namespace Vectorize.Services
{
    public class MongoDbService
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ILogger _logger;

        public MongoDbService(string connection, string databaseName, string collectionName, ILogger logger)
        {

            _client = new MongoClient(connection);
            _database = _client.GetDatabase(databaseName);
            _collection = _database.GetCollection<BsonDocument>(collectionName);
            _logger = logger;

            string vectorIndexName = "vectorSearchIndex";
            
            //Find if vector index exists
            var indexes = _collection.Indexes.List().ToList();
            var indexExists = indexes.Find(i => i["name"].AsString == vectorIndexName);


            if (indexExists is null) 
            {
                
                //To-Do: Build this string dynamically to allow for user defined values
                //'vectors' is the collection name. the property is 'cosmosSearch'
                
                BsonDocumentCommand<BsonDocument> command = new BsonDocumentCommand<BsonDocument>(
                BsonDocument.Parse("{createIndexes: 'vectors', indexes: [{ name: 'vectorSearchIndex', key: { vector: 'cosmosSearch' }, cosmosSearchOptions: { kind: 'vector-ivf', numLists: 5, similarity: 'COS', dimensions: 3 } }] }"));
                
                _database.RunCommand(command);
            }

            //Register new serializer to handle Cosmos JSON documents /id property.
            BsonSerializer.RegisterSerializer(new JsonToBsonSerializer());
        }

        
        public async Task InsertVector(BsonDocument document)
        {

            try 
            {

                await _collection.InsertOneAsync(document);

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.Message);
            }

        }


        public async Task UpsertVector(BsonDocument document)
        {

            throw new Exception("not implemented");

            /*
            try
            {

                var filter = Builders<MyDocument>.Filter.Eq(x => x.Id, myId);
                var options = new ReplaceOptions { IsUpsert = true };
                await _collection.ReplaceOne(filter, newDocument, options);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            */
        }

    }

    public class JsonToBsonSerializer : SerializerBase<dynamic>
    {
        public override dynamic Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return BsonSerializer.Deserialize<dynamic>(context.Reader);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, dynamic value)
        {
            var bsonDocument = new BsonDocument();
            BsonSerializer.Serialize(context.Writer, value.GetType(), value);

            bsonDocument.AddRange(value.ToBsonDocument());

            if (value.Id != ObjectId.Empty)
            {
                bsonDocument["_id"] = value.id;
            }

            context.Writer.WriteEndDocument();
        }
    }
}
