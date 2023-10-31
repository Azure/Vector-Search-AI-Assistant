using System.Collections;
using System.Reflection;
using System.Text.Json;
using Azure;
using Azure.Core.Serialization;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Search;

namespace VectorSearchAiAssistant.Service.Services
{
    /// <summary>
    /// This service is provided as an example, it is not used by the main RAG flow.
    /// The capabilities showcased by this class are used in the AzureCognitiveSearchVectorMemory class.
    /// </summary>
    public class CognitiveSearchService : IVectorDatabaseServiceManagement, IVectorDatabaseServiceQueries
    {
        private const int ModelDimensions = 1536;
        private const string VectorFieldName = "vector";
        private readonly int _maxVectorSearchResults = default;
        private readonly ILogger _logger;
        private readonly SearchClient _searchClient;

        public CognitiveSearchService(string azureSearchAdminKey, string azureSearchServiceEndpoint,
            string azureSearchIndexName, string maxVectorSearchResults, ILogger logger, bool createIndexIfNotExists = false)
        {
            _maxVectorSearchResults = int.TryParse(maxVectorSearchResults, out _maxVectorSearchResults) ? _maxVectorSearchResults : 10;
            _logger = logger;

            // Define client options to use camelCase when serializing property names.
            var options = new SearchClientOptions()
            {
                Serializer = new JsonObjectSerializer(
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })
            };

            var searchCredential = new AzureKeyCredential(azureSearchAdminKey);
            var indexClient = new SearchIndexClient(new Uri(azureSearchServiceEndpoint), searchCredential, options);
            _searchClient = indexClient.GetSearchClient(azureSearchIndexName);

            if (!createIndexIfNotExists) return;
            // If the Azure Cognitive Search index does not exists, create the index.
            try
            {
                CreateIndexAsync(indexClient, azureSearchIndexName, true).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError("Azure Cognitive Search index creation failure: " + ex.Message);
                throw;
            }
        }

        public async Task InsertVector(object document)
        {
            await InsertVectors(new[] { document });
        }

        public async Task InsertVectors(IEnumerable<object> documents)
        {
            try
            {
                await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(documents));
                _logger.LogInformation("Inserted new vectors into Cognitive Search");
            }
            catch (Exception ex)
            {
                //TODO: fix the logger. Output does not show up anywhere
                _logger.LogError($"Exception: InsertVectors(): {ex.Message}");
                throw;
            }
        }

        public async Task DeleteVector(object document)
        {
            try
            {
                var objectType = document.GetType();
                var properties = objectType.GetProperties();

                foreach (var property in properties)
                {
                    var searchableAttribute = property.GetCustomAttribute<SearchableFieldAttribute>();
                    if (searchableAttribute != null && searchableAttribute.IsKey)
                    {
                        var propertyName = property.Name;
                        var propertyValue = property.GetValue(document);

                        Console.WriteLine($"Found key property: {propertyName}, Value: {propertyValue}");
                        await _searchClient.DeleteDocumentsAsync(propertyName, new[] { propertyValue?.ToString() });

                        _logger.LogInformation("Deleted vector from Cognitive Search");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO: fix the logger. Output does not show up anywhere
                _logger.LogError($"Exception: DeleteVector(): {ex.Message}");
                throw;
            }
        }

        public async Task<string> VectorSearchAsync(float[] embeddings)
        {
            var retDocs = new List<string>();

            string resultDocuments = string.Empty;

            try
            {
                // Perform the vector similarity search  
                var vector = new SearchQueryVector { K = _maxVectorSearchResults, Fields = VectorFieldName, Value = embeddings };
                var searchOptions = new SearchOptions
                {
                    Vector = vector,
                    Size = _maxVectorSearchResults
                };

                SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(null, searchOptions);

                var count = 0;
                var serializerOptions = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
                await foreach (var result in response.GetResultsAsync())
                {
                    count++;
                    var filteredDocument = new SearchDocument();
                    var searchDocument = result.Document;
                    foreach (var property in searchDocument)
                    {
                        // Exclude null properties, empty arrays/lists, and the "vector" property.
                        // This helps minimize the amount of data and also eliminates fields that may only relate to other document types.
                        if (property.Value != null && property.Key != VectorFieldName && !IsEmptyArrayOrList(property.Value))
                        {
                            filteredDocument[property.Key] = property.Value;
                        }
                    }
                    //logger.LogInformation(filteredDocument);
                    //logger.LogInformation($"Score: {result.Score}\n");

                    retDocs.Add(JsonSerializer.Serialize(filteredDocument, serializerOptions));
                }
                resultDocuments = string.Join(Environment.NewLine + "-", retDocs);
                _logger.LogInformation($"Total Results: {count}");

            }
            catch (Exception ex)
            {
                _logger.LogError($"There was an error conducting a vector search: {ex.Message}");
            }

            return resultDocuments;
        }

        // Helper method to check if a value is an empty array or list.
        // TODO: Move this to a helper utility within a shared library.
        private bool IsEmptyArrayOrList(object value)
        {
            if (value is Array array)
            {
                return array.Length == 0;
            }

            if (value is IList list)
            {
                return list.Count == 0;
            }

            return false;
        }

        internal async Task CreateIndexAsync(SearchIndexClient indexClient, string indexName,
            bool onlyCreateIfNotExists)
        {
            try
            {
                if (onlyCreateIfNotExists)
                {
                    if (await indexClient.GetIndexAsync(indexName) != null)
                    {
                        _logger.LogInformation($"The {indexName} index already exists; skipping index creation.");
                        return;
                    }
                }

                var vectorSearchConfigName = "vector-config";
                
                var fieldBuilder = new FieldBuilder();
                var customerFields = fieldBuilder.Build(typeof(Customer));
                var productFields = fieldBuilder.Build(typeof(Product));
                var salesOrderFields = fieldBuilder.Build(typeof(SalesOrder));

                // Combine the three search fields, eliminating duplicate names:
                var allFields = customerFields
                    .Concat(productFields)
                    .Concat(salesOrderFields)
                    .GroupBy(field => field.Name)
                    .Select(group => group.First())
                    .ToList();
                allFields.Add(
                    new SearchField(VectorFieldName, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        Dimensions = ModelDimensions,
                        VectorSearchConfiguration = vectorSearchConfigName
                    });

                SearchIndex searchIndex = new(indexName)
                {
                    VectorSearch = new()
                    {
                        AlgorithmConfigurations =
                        {
                            new VectorSearchAlgorithmConfiguration(vectorSearchConfigName, "hnsw")
                        }
                    },
                    Fields = allFields
                };

                await indexClient.CreateIndexAsync(searchIndex);
                _logger.LogInformation($"Created the {indexName} index.");
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occurred while trying to build the {indexName} index: {e}");
                throw;
            }
        }
    }
}
