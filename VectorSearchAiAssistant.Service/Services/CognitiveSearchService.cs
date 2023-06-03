using System.Collections;
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
    public class CognitiveSearchService : ICognitiveSearchServiceManagement, ICognitiveSearchServiceQueries
    {
        private const int ModelDimensions = 1536;
        private const string VectorFieldName = "vector";
        private readonly int _maxVectorSearchResults = default;
        private readonly SearchClient _searchClient;

        public CognitiveSearchService(string azureSearchAdminKey, string azureSearchServiceEndpoint,
            string azureSearchIndexName, string maxVectorSearchResults, ILogger logger)
        {
            _maxVectorSearchResults = int.TryParse(maxVectorSearchResults, out _maxVectorSearchResults) ? _maxVectorSearchResults : 10;

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

            // If the Azure Cognitive Search index does not exists, create the index.
            try
            {
                CreateIndexAsync(indexClient, azureSearchIndexName, true, logger).Wait();
            }
            catch (Exception ex)
            {
                logger.LogError("Azure Cognitive Search index creation failure: " + ex.Message);
                throw;
            }
        }

        public async Task InsertVector(object document, ILogger logger)
        {
            await InsertVectors(new[] { document }, logger);
        }

        public async Task InsertVectors(IEnumerable<object> documents, ILogger logger)
        {
            try
            {
                await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(documents));
                logger.LogInformation("Inserted new vectors into Cognitive Search");
            }
            catch (Exception ex)
            {
                //TODO: fix the logger. Output does not show up anywhere
                logger.LogError($"Exception: InsertVectors(): {ex.Message}");
                throw;
            }
        }

        public async Task DeleteVector(object document, ILogger logger)
        {
            try
            {
                await _searchClient.DeleteDocumentsAsync(new[] { document });
                logger.LogInformation("Deleted vector from Cognitive Search");
            }
            catch (Exception ex)
            {
                //TODO: fix the logger. Output does not show up anywhere
                logger.LogError($"Exception: DeleteVector(): {ex.Message}");
                throw;
            }
        }

        public async Task<string> VectorSearchAsync(float[] embeddings, ILogger logger)
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
                logger.LogInformation($"Total Results: {count}");

            }
            catch (Exception ex)
            {
                logger.LogError($"There was an error conducting a vector search: {ex.Message}");
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
            bool onlyCreateIfNotExists, ILogger logger)
        {
            if (onlyCreateIfNotExists)
            {
                await foreach (var result in indexClient.GetIndexNamesAsync())
                {
                    if (string.Equals(result.ToLower(), indexName.ToLower(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        logger.LogInformation($"The {indexName} index already exists; skipping index creation.");
                        return;
                    }
                }
            }

            var vectorSearchConfigName = "vector-config";

            try
            {
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
                logger.LogInformation($"Created the {indexName} index.");
            }
            catch (Exception e)
            {
                logger.LogError($"An error occurred while trying to build the {indexName} index: {e}");
                throw;
            }
        }
    }
}
