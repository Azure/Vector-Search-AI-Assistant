using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Memory;
using VectorSearchAiAssistant.SemanticKernel.TextEmbedding;

namespace VectorSearchAiAssistant.SemanticKernel.Plugins.Memory
{
    public class VectorMemoryStore
    {
        readonly string _collectionName;
        readonly IMemoryStore _memoryStore;
        readonly ITextEmbeddingGeneration _textEmbedding;
        readonly ILogger<VectorMemoryStore> _logger;

        public VectorMemoryStore(
            string collectionName,
            IMemoryStore memoryStore,
            ITextEmbeddingGeneration textEmbedding,
            ILogger<VectorMemoryStore> logger) 
        {
            _collectionName = collectionName;
            _memoryStore = memoryStore;
            _textEmbedding = textEmbedding;
            _logger = logger;
        }

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
                vectorizer(item, embbedding.ToArray());

                // This will send the vectorized object to the Azure Cognitive Search index.
                await _memoryStore.UpsertAsync(_collectionName, new MemoryRecord();

                await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(new object[] { item }));

                _logger.LogInformation($"Saved vector for item: {itemName} of type {item.GetType().Name}");
            }
            catch (Exception x)
            {
                _logger.LogError($"Exception while generating vector for [{itemName} of type {item.GetType().Name}]: " + x.Message);
            }
        }
    }
}
