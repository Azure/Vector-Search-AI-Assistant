using Azure.Core;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using Microsoft.SemanticKernel.Memory;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.AI.Embeddings;
using System.Text.Json;
using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Azure.Core.Serialization;
using System.Reflection.Metadata;
using VectorSearchAiAssistant.SemanticKernel.TextEmbedding;

namespace VectorSearchAiAssistant.SemanticKernel.Memory.AzureCognitiveSearch
{
    /// <summary>
    /// Semantic Memory implementation using Azure Cognitive Search.
    /// For more information about Azure Cognitive Search see https://learn.microsoft.com/azure/search/search-what-is-azure-search
    /// </summary>
    public class AzureCognitiveSearchVectorMemory : ISemanticTextMemory
    {
        private readonly SearchIndexClient _adminClient;
        private SearchClient _searchClient;
        private readonly string _searchIndexName;

        private readonly ConcurrentDictionary<string, SearchClient> _clientsByIndex = new();

        private readonly ITextEmbeddingGeneration _textEmbedding;
        private readonly ILogger _logger;

        private const string VectorFieldName = "vector";
        private const int ModelDimensions = 1536;

        /// <summary>
        /// Create a new instance of semantic memory using Azure Cognitive Search.
        /// </summary>
        /// <param name="endpoint">Azure Cognitive Search URI, e.g. "https://contoso.search.windows.net"</param>
        /// <param name="apiKey">API Key</param>
        public AzureCognitiveSearchVectorMemory(string endpoint, string apiKey, string indexName, ITextEmbeddingGeneration textEmbedding, ILogger logger)
        {
            AzureKeyCredential credentials = new(apiKey);

            _adminClient = new SearchIndexClient(new Uri(endpoint), credentials, GetSearchClientOptions());
            _searchIndexName = indexName;
            _textEmbedding = textEmbedding;
            _logger = logger;
        }

        /// <summary>
        /// Create a new instance of semantic memory using Azure Cognitive Search.
        /// </summary>
        /// <param name="endpoint">Azure Cognitive Search URI, e.g. "https://contoso.search.windows.net"</param>
        /// <param name="credentials">Azure service</param>
        public AzureCognitiveSearchVectorMemory(string endpoint, TokenCredential credentials, string indexName, ITextEmbeddingGeneration textEmbedding, ILogger logger)
        {
            _adminClient = new SearchIndexClient(new Uri(endpoint), credentials, GetSearchClientOptions());
            _textEmbedding = textEmbedding;
        }

        /// <summary>
        /// Initialize the memory by creating the underlying Azure Cognitive Search index.
        /// </summary>
        /// <param name="typesToIndex">The object types supported by the index.</param>
        /// <returns></returns>
        public async Task Initialize(List<Type> typesToIndex)
        {
            /* TODO: Challenge 2.  
             * Uncomment and complete the following lines as instructed.
             */

            try
            {
                var indexNames = await _adminClient.GetIndexNamesAsync().ToListAsync().ConfigureAwait(false);
                if (indexNames.Contains(_searchIndexName))
                {
                    _searchClient = _adminClient.GetSearchClient(_searchIndexName);
                    _logger.LogInformation($"The {_searchIndexName} index already exists; skipping index creation.");
                    return;
                }

                var vectorSearchConfigName = "vector-config";

                var fieldBuilder = new FieldBuilder();

                var fieldsToIndex = typesToIndex
                    .Select(tti => fieldBuilder.Build(tti))
                    .SelectMany(x => x);

                // Combine the search fields, eliminating duplicate names:
                var allFields = fieldsToIndex
                    .GroupBy(field => field.Name)
                    .Select(group => group.First())
                    .ToList();

                // TODO: Create the Cognitive Search vector SearchFields for the list of distinct fields across all types
                // Make sure that all fields are searchable, have the appropriate dimensions (1536) 
                // and use the configuration specified by the vectorSearchConfigName variable.
                //allFields.Add(
                //    new SearchField(VectorFieldName, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                //    {
                //        IsSearchable = _____,
                //        Dimensions = _____,
                //        VectorSearchConfiguration = vectorSearchConfigName
                //    });

                // TODO: Replace the following line with the TODO that follows.
                SearchIndex searchIndex = new(_searchIndexName);
                // TODO: Create the SearchIndex to use the VectorSearchAlgorithmConfiguration with
                // the vectorSearchConfigName and the "hnsw" kind
                //SearchIndex searchIndex = new(_searchIndexName)
                //{
                //    VectorSearch = new()
                //    {
                //        AlgorithmConfigurations =
                //        {
                //            new ______(vectorSearchConfigName, "hnsw")
                //        }
                //    },
                //    Fields = ______
                //};

                await _adminClient.CreateIndexAsync(searchIndex);
                _searchClient = _adminClient.GetSearchClient(_searchIndexName);

                _logger.LogInformation($"Created the {_searchIndexName} index.");
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occurred while trying to build the {_searchIndexName} index: {e}");
            }
        }

        /// <summary>
        /// Add an object instance and its associated vectorization to the underlying memory.
        /// </summary>
        /// <param name="item">The object instance to be added to the memory.</param>
        /// <param name="itemName">The name of the object instance.</param>
        /// <param name="vectorizer">The logic that sets the embedding vector as a property on the object.</param>
        /// <returns></returns>
        public async Task AddMemory(object item, string itemName, Action<object, float[]> vectorizer)
        {
            try
            {
                // Prepare the object for embedding
                var itemToEmbed = EmbeddingUtility.Transform(item);

                // Get the embeddings from OpenAI: the ITextEmbeddingGeneration service is exposed by SemanticKernel
                // and is responsible for calling the text embedding endpoint to get the vectorized representation
                // of the incoming object.
                // Use by default the more elaborate text representation based on EmbeddingFieldAttribute
                // The purely text representation generated based on the EmbeddingFieldAttribute is well suited for 
                // embedding and it allows you to control precisely which attributes will be used as inputs in the process.
                // In general, it is recommended to avoid identifier attributes (e.g., GUIDs) as they do not provide
                // any meaningful context for the embedding process.
                // Exercise: Test also using the JSON text representation - itemToEmbed.ObjectToEmbed
                var embbedding = await _textEmbedding.GenerateEmbeddingAsync(itemToEmbed.TextToEmbed);

                // Add the newly calculated embedding to the entity.
                vectorizer(item, embbedding.Vector.ToArray());

                // This will send the vectorized object to the Azure Cognitive Search index.
                await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(new object[] { item }));

                _logger.LogInformation($"Saved vector for item: {itemName} of type {item.GetType().Name}");
            }
            catch (Exception x)
            {
                _logger.LogError($"Exception while generating vector for [{itemName} of type {item.GetType().Name}]: " + x.Message);
            }
        }

        public async Task RemoveMemory(object item)
        {
            try
            {
                var objectType = item.GetType();
                var properties = objectType.GetProperties();

                foreach (var property in properties)
                {
                    var searchableAttribute = property.GetCustomAttribute<SearchableFieldAttribute>();
                    if (searchableAttribute != null && searchableAttribute.IsKey)
                    {
                        var propertyName = property.Name;
                        var propertyValue = property.GetValue(item);

                        Console.WriteLine($"Found key property: {propertyName}, Value: {propertyValue}");
                        await _searchClient.DeleteDocumentsAsync(propertyName, new[] { propertyValue?.ToString() });

                        _logger.LogInformation("Deleted vector from Cognitive Search");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: DeleteVector(): {ex.Message}");
            }
        }

        /// <inheritdoc />
        public Task<string> SaveInformationAsync(
            string collection,
            string text,
            string id,
            string? description = null,
            string? additionalMetadata = null,
            CancellationToken cancellationToken = default)
        {
            collection = NormalizeIndexName(collection);

            AzureCognitiveSearchRecord record = new()
            {
                Id = EncodeId(id),
                Text = text,
                Description = description,
                AdditionalMetadata = additionalMetadata,
                IsReference = false,
            };

            return UpsertRecordAsync(collection, record, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> SaveReferenceAsync(
            string collection,
            string text,
            string externalId,
            string externalSourceName,
            string? description = null,
            string? additionalMetadata = null,
            CancellationToken cancellationToken = default)
        {
            collection = NormalizeIndexName(collection);

            AzureCognitiveSearchRecord record = new()
            {
                Id = EncodeId(externalId),
                Text = text,
                Description = description,
                AdditionalMetadata = additionalMetadata,
                ExternalSourceName = externalSourceName,
                IsReference = true,
            };

            return UpsertRecordAsync(collection, record, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<MemoryQueryResult?> GetAsync(
            string collection,
            string key,
            bool withEmbedding = false,
            CancellationToken cancellationToken = default)
        {
            collection = NormalizeIndexName(collection);

            var client = GetSearchClient(collection);

            Response<AzureCognitiveSearchRecord>? result;
            try
            {
                result = await client
                    .GetDocumentAsync<AzureCognitiveSearchRecord>(EncodeId(key), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                // Index not found, no data to return
                return null;
            }

            if (result?.Value == null)
            {
                throw new AzureCognitiveSearchMemoryException("Memory read returned null");
            }

            return new MemoryQueryResult(ToMemoryRecordMetadata(result.Value), 1, null);
        }

        /// <summary>
        /// Retrieve records from the underlying index that are above a specified similarity threshold when compared to a given string.
        /// </summary>
        /// <param name="collection">The name of the index.</param>
        /// <param name="query">The string to be vectorized and compared to the items in the underlying index.</param>
        /// <param name="limit">The maximum number of items to return.</param>
        /// <param name="minRelevanceScore">The lower limit of the similarity score.</param>
        /// <param name="withEmbeddings"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async IAsyncEnumerable<MemoryQueryResult> SearchAsync(
            string collection,
            string query,
            int limit = 1,
            double minRelevanceScore = 0.7,
            bool withEmbeddings = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (collection.CompareTo(_searchIndexName) != 0)
                throw new ArgumentException($"There is no corresponding index for collection {collection}.");

            collection = NormalizeIndexName(collection);

            var client = GetSearchClient(collection);

            if (withEmbeddings)
            {
                var embedding = await _textEmbedding.GenerateEmbeddingAsync(query);

                // Perform the vector similarity search  
                var vector = new SearchQueryVector { K = limit, Fields = VectorFieldName, Value = embedding.Vector.ToList() };
                var searchOptions = new SearchOptions
                {
                    Vector = vector,
                    Size = limit
                };

                SearchResults<SearchDocument> searchResult = null;
                try
                {
                    searchResult = await client.SearchAsync<SearchDocument>(null, searchOptions);
                }
                catch (RequestFailedException e) when (e.Status == 404)
                {
                    // Index not found, no data to return
                }

                //By convention, the first item in the result is the embedding of the query.
                //Once SK develops a more standardized way to expose embeddings, this should be removed.
                yield return new MemoryQueryResult(null, 1, embedding);

                if (searchResult != null)
                {
                    await foreach (SearchResult<SearchDocument> result in searchResult.GetResultsAsync())
                    {
                        if (result.Score < minRelevanceScore) { break; }

                        yield return new MemoryQueryResult(ToMemoryRecordMetadata(result), result.Score ?? 1, null);
                    }
                }
            }
            else
            {
                var options = new SearchOptions
                {
                    QueryType = SearchQueryType.Semantic,
                    SemanticConfigurationName = "default",
                    QueryLanguage = "en-us",
                    Size = limit,
                };

                Response<SearchResults<AzureCognitiveSearchRecord>>? searchResult = null;
                try
                {
                    searchResult = await client
                        .SearchAsync<AzureCognitiveSearchRecord>(query, options, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (RequestFailedException e) when (e.Status == 404)
                {
                    // Index not found, no data to return
                }

                if (searchResult != null)
                {
                    await foreach (SearchResult<AzureCognitiveSearchRecord>? doc in searchResult.Value.GetResultsAsync())
                    {
                        if (doc.RerankerScore < minRelevanceScore) { break; }

                        yield return new MemoryQueryResult(ToMemoryRecordMetadata(doc.Document), doc.RerankerScore ?? 1, null);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string collection, string key, CancellationToken cancellationToken = default)
        {
            collection = NormalizeIndexName(collection);

            var records = new List<AzureCognitiveSearchRecord> { new() { Id = EncodeId(key) } };

            var client = GetSearchClient(collection);
            try
            {
                await client.DeleteDocumentsAsync(records, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                // Index not found, no data to delete
            }
        }

        /// <inheritdoc />
        public async Task<IList<string>> GetCollectionsAsync(CancellationToken cancellationToken = default)
        {
            ConfiguredCancelableAsyncEnumerable<SearchIndex> indexes = _adminClient.GetIndexesAsync(cancellationToken).ConfigureAwait(false);

            var result = new List<string>();
            await foreach (var index in indexes)
            {
                result.Add(index.Name);
            }

            return result;
        }

        #region private ================================================================================

        /// <summary>
        /// Index names cannot contain special chars. We use this rule to replace a few common ones
        /// with an underscore and reduce the chance of errors. If other special chars are used, we leave it
        /// to the service to throw an error.
        /// Note:
        /// - replacing chars introduces a small chance of conflicts, e.g. "the-user" and "the_user".
        /// - we should consider whether making this optional and leave it to the developer to handle.
        /// </summary>
        private static readonly Regex s_replaceIndexNameSymbolsRegex = new(@"[\s|\\|/|.|_|:]");

        /// <summary>
        /// Get a search client for the index specified.
        /// Note: the index might not exist, but we avoid checking everytime and the extra latency.
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <returns>Search client ready to read/write</returns>
        private SearchClient GetSearchClient(string indexName)
        {
            // Search an available client from the local cache
            if (!_clientsByIndex.TryGetValue(indexName, out SearchClient client))
            {
                client = _adminClient.GetSearchClient(indexName);
                _clientsByIndex[indexName] = client;
            }

            return client;
        }

        /// <summary>
        /// Create a new search index.
        /// </summary>
        /// <param name="indexName">Index name</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        private Task<Response<SearchIndex>> CreateIndexAsync(
            string indexName,
            CancellationToken cancellationToken = default)
        {
            var fieldBuilder = new FieldBuilder();
            var fields = fieldBuilder.Build(typeof(AzureCognitiveSearchRecord));
            var newIndex = new SearchIndex(indexName, fields)
            {
                SemanticSettings = new SemanticSettings
                {
                    Configurations =
                    {
                        // TODO: replace with vector search
                        new SemanticConfiguration("default", new PrioritizedFields
                        {
                            TitleField = new SemanticField { FieldName = "Description" },
                            ContentFields =
                            {
                                new SemanticField { FieldName = "Text" },
                                new SemanticField { FieldName = "AdditionalMetadata" },
                            }
                        })
                    }
                }
            };

            return _adminClient.CreateIndexAsync(newIndex, cancellationToken);
        }

        private async Task<string> UpsertRecordAsync(
            string indexName,
            AzureCognitiveSearchRecord record,
            CancellationToken cancellationToken = default)
        {
            var client = GetSearchClient(indexName);

            Task<Response<IndexDocumentsResult>> UpsertCode() => client
                .MergeOrUploadDocumentsAsync(new List<AzureCognitiveSearchRecord> { record },
                    new IndexDocumentsOptions { ThrowOnAnyError = true },
                    cancellationToken);

            Response<IndexDocumentsResult>? result;
            try
            {
                result = await UpsertCode().ConfigureAwait(false);
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                await CreateIndexAsync(indexName, cancellationToken).ConfigureAwait(false);
                result = await UpsertCode().ConfigureAwait(false);
            }

            if (result == null || result.Value.Results.Count == 0)
            {
                throw new AzureCognitiveSearchMemoryException("Memory write returned null or an empty set");
            }

            return result.Value.Results[0].Key;
        }

        private static MemoryRecordMetadata ToMemoryRecordMetadata(AzureCognitiveSearchRecord data)
        {
            return new MemoryRecordMetadata(
                isReference: data.IsReference,
                id: DecodeId(data.Id),
                text: data.Text ?? string.Empty,
                description: data.Description ?? string.Empty,
                externalSourceName: data.ExternalSourceName,
                additionalMetadata: data.AdditionalMetadata ?? string.Empty);
        }

        private static MemoryRecordMetadata ToMemoryRecordMetadata(SearchResult<SearchDocument> data)
        {
            var filteredDocument = new SearchDocument();
            var searchDocument = data.Document;
            foreach (var property in searchDocument)
            {
                // Exclude null properties, empty arrays/lists, and the "vector" property.
                // This helps minimize the amount of data and also eliminates fields that may only relate to other document types.
                if (property.Value != null && property.Key != VectorFieldName && !IsEmptyArrayOrList(property.Value))
                {
                    filteredDocument[property.Key] = property.Value;
                }
            }

            return new MemoryRecordMetadata(
                isReference: false,
                id: filteredDocument["id"].ToString(),
                text: string.Empty,
                description: string.Empty,
                externalSourceName: string.Empty,
                additionalMetadata: JsonSerializer.Serialize(filteredDocument, new JsonSerializerOptions { WriteIndented = false }));
        }

        private static bool IsEmptyArrayOrList(object value)
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

        /// <summary>
        /// Normalize index name to match ACS rules.
        /// The method doesn't handle all the error scenarios, leaving it to the service
        /// to throw an error for edge cases not handled locally.
        /// </summary>
        /// <param name="indexName">Value to normalize</param>
        /// <returns>Normalized name</returns>
        private static string NormalizeIndexName(string indexName)
        {
            if (indexName.Length > 128)
            {
                throw new AzureCognitiveSearchMemoryException("The collection name is too long, it cannot exceed 128 chars");
            }

#pragma warning disable CA1308 // The service expects a lowercase string
            indexName = indexName.ToLowerInvariant();
#pragma warning restore CA1308

            return s_replaceIndexNameSymbolsRegex.Replace(indexName.Trim(), "-");
        }

        /// <summary>
        /// ACS keys can contain only letters, digits, underscore, dash, equal sign, recommending
        /// to encode values with a URL-safe algorithm.
        /// </summary>
        /// <param name="realId">Original Id</param>
        /// <returns>Encoded id</returns>
        private static string EncodeId(string realId)
        {
            var bytes = Encoding.UTF8.GetBytes(realId);
            return Convert.ToBase64String(bytes);
        }

        private static string DecodeId(string encodedId)
        {
            var bytes = Convert.FromBase64String(encodedId);
            return Encoding.UTF8.GetString(bytes);
        }

        private SearchClientOptions GetSearchClientOptions()
        {
            return new SearchClientOptions()
            {
                Serializer = new JsonObjectSerializer(
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })
            };
        }

        #endregion
    }

}