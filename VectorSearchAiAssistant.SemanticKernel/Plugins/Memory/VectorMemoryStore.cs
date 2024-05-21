using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Newtonsoft.Json;
using System.Reflection;
using VectorSearchAiAssistant.Common.Interfaces;

#pragma warning disable SKEXP0001

namespace VectorSearchAiAssistant.SemanticKernel.Plugins.Memory
{
    public class VectorMemoryStore
    {
        readonly string _collectionName;
        readonly IMemoryStore _memoryStore;
        readonly ITextEmbeddingGenerationService _textEmbedding;
        readonly ILogger<VectorMemoryStore> _logger;

        public IMemoryStore MemoryStore => _memoryStore;

        public VectorMemoryStore(
            string collectionName,
            IMemoryStore memoryStore,
            ITextEmbeddingGenerationService textEmbedding,
            ILogger<VectorMemoryStore> logger) 
        {
            _collectionName = collectionName;
            _memoryStore = memoryStore;
            _textEmbedding = textEmbedding;
            _logger = logger;
        }

        public async Task Initialize() =>
            await _memoryStore.CreateCollectionAsync(_collectionName);

        public async Task AddMemory(string id, string memory, ReadOnlyMemory<float> memoryEmbedding, string? metadata = null, string? key = null) =>
            await _memoryStore.UpsertAsync(_collectionName, MemoryRecord.LocalRecord(
                id,
                memory,
                string.Empty,
                memoryEmbedding,
                metadata,
                key));

        public async Task AddMemory(IItemTransformer itemTransformer)
        {
            try
            {    
                // Get the embeddings from OpenAI: the ITextEmbeddingGenerationService service is exposed by SemanticKernel
                // and is responsible for calling the text embedding endpoint to get the vectorized representation
                // of the incoming object.
                // Use by default the more elaborate text representation based on the ModelRegistryItemTransformer which relies on the EmbeddingFieldAttribute attribute.
                // The purely text representation generated based on the EmbeddingFieldAttribute is well suited for 
                // embedding and it allows you to control precisely which attributes will be used as inputs in the process.
                // In general, it is recommended to avoid identifier attributes (e.g., GUIDs) as they do not provide
                // any meaningful context for the embedding process.
                // Exercise: Test also using the JSON text representation - itemTransformer.ObjectToEmbed
                var embedding = await _textEmbedding.GenerateEmbeddingAsync(itemTransformer.TextToEmbed);

                await _memoryStore.UpsertAsync(_collectionName, MemoryRecord.LocalRecord(
                    itemTransformer.EmbeddingId,
                    itemTransformer.TextToEmbed,
                    string.Empty,
                    embedding,
                    JsonConvert.SerializeObject(itemTransformer.TypedValue),
                    itemTransformer.EmbeddingPartitionKey));

                _logger.LogInformation($"Memorized vector for item: {itemTransformer.Name} of type {itemTransformer.TypedValue.GetType().Name}");
            }
            catch (Exception x)
            {
                _logger.LogError($"Exception while generating vector for [{itemTransformer.Name} of type {itemTransformer.TypedValue.GetType().Name}]: " + x.Message);
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

                        _logger.LogInformation($"Found key property: {propertyName}, Value: {propertyValue}");
                        await _memoryStore.RemoveAsync(_collectionName, propertyValue?.ToString());

                        _logger.LogInformation("Removed memory successfully.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: RemoveMemory(): {ex.Message}");
            }
        }

        public async IAsyncEnumerable<MemoryQueryResult> GetNearestMatches(string textToMatch, int limit, double minRelevanceScore = 0.7)
        {
            var embedding = await _textEmbedding.GenerateEmbeddingAsync(textToMatch);
            await foreach (var result in _memoryStore.GetNearestMatchesAsync(
                _collectionName,
                embedding,
                limit,
                minRelevanceScore))
            {
                yield return new MemoryQueryResult(result.Item1.Metadata, result.Item2, null);
            }
        }

        public async IAsyncEnumerable<MemoryQueryResult> GetNearestMatches(ReadOnlyMemory<float> embeddingToMatch, int limit, double minRelevanceScore = 0.7)
        {
            await foreach (var result in _memoryStore.GetNearestMatchesAsync(
            _collectionName,
                embeddingToMatch,
                limit,
                minRelevanceScore))
            {
                yield return new MemoryQueryResult(result.Item1.Metadata, result.Item2, null);
            }
        }

        public async Task<ReadOnlyMemory<float>> GetEmbedding(string textToEmbed)
        {
            return await _textEmbedding.GenerateEmbeddingAsync(textToEmbed);
        }
    }
}
