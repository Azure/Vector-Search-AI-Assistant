using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Newtonsoft.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using VectorSearchAiAssistant.Common.Models.BusinessDomain;
using VectorSearchAiAssistant.Common.Text;

#pragma warning disable SKEXP0001

namespace VectorSearchAiAssistant.SemanticKernel.Plugins.Memory
{
    public class VectorMemoryStore
    {
        readonly string _collectionName;
        readonly IMemoryStore _memoryStore;
        readonly ITextEmbeddingGenerationService _textEmbedding;
        readonly ILogger<VectorMemoryStore> _logger;
        readonly SHA1 _hash;

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
            _hash = SHA1.Create();
        }

        public async Task AddMemory(string memory, ReadOnlyMemory<float> memoryEmbedding, string metadata = "")
        {
            await UpdateMemoryStore(
                GetHash(memory),
                memory,
                memoryEmbedding,
                metadata);
        }

        public async Task AddMemory(string memory, string metadata  ="")
        {
            var memoryEmbedding = await _textEmbedding.GenerateEmbeddingAsync(memory);
            await UpdateMemoryStore(
                GetHash(memory),
                memory,
                memoryEmbedding,
                metadata);
        }

        public async Task AddMemory(object item, string itemName)
        {
            try
            {
                if (item is EmbeddedEntity entity)
                    entity.entityType__ = item.GetType().Name;
                else
                    throw new ArgumentException("Only objects derived from EmbeddedEntity can be added to memory.");

                // Prepare the object for embedding
                var itemToEmbed = EmbeddingUtility.Transform(item);

                // Get the embeddings from OpenAI: the ITextEmbeddingGenerationService service is exposed by SemanticKernel
                // and is responsible for calling the text embedding endpoint to get the vectorized representation
                // of the incoming object.
                // Use by default the more elaborate text representation based on EmbeddingFieldAttribute
                // The purely text representation generated based on the EmbeddingFieldAttribute is well suited for 
                // embedding and it allows you to control precisely which attributes will be used as inputs in the process.
                // In general, it is recommended to avoid identifier attributes (e.g., GUIDs) as they do not provide
                // any meaningful context for the embedding process.
                // Exercise: Test also using the JSON text representation - itemToEmbed.ObjectToEmbed
                var embedding = await _textEmbedding.GenerateEmbeddingAsync(itemToEmbed.TextToEmbed);

                await UpdateMemoryStore(
                    itemToEmbed.ObjectToEmbed.ContainsKey("id")
                            ? itemToEmbed.ObjectToEmbed.Value<string>("id")
                            : GetHash(itemToEmbed.TextToEmbed),
                    itemToEmbed.TextToEmbed,
                    embedding,
                    JsonConvert.SerializeObject(item));

                _logger.LogInformation($"Memorized vector for item: {itemName} of type {item.GetType().Name}");
            }
            catch (Exception x)
            {
                _logger.LogError($"Exception while generating vector for [{itemName} of type {item.GetType().Name}]: " + x.Message);
            }
        }

        #region Helpers for AddMemory

        private async Task UpdateMemoryStore(string id, string text, ReadOnlyMemory<float> embedding, string metadata = "") =>
            await _memoryStore.UpsertAsync(_collectionName, new MemoryRecord(
                new MemoryRecordMetadata(
                    false,
                    id,
                    text,
                    string.Empty,
                    string.Empty,
                    metadata),
                embedding,
                null));

        #endregion

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

        private string GetHash(string s)
        {
            return Convert.ToBase64String(
                _hash.ComputeHash(
                    Encoding.UTF8.GetBytes(s)));
        }
    }
}
