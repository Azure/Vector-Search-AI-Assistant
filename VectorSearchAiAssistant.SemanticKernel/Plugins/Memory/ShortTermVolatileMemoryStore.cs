using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using VectorSearchAiAssistant.SemanticKernel.TextEmbedding;

namespace VectorSearchAiAssistant.SemanticKernel.Plugins.Memory
{
    public class ShortTermVolatileMemoryStore : VolatileMemoryStore
    {
        private List<object> _memories;
        private readonly ITextEmbeddingGeneration _textEmbedding;
        private readonly ILogger _logger;
        private readonly string _collectionName;
        private readonly SHA1 _hash;

        public ShortTermVolatileMemoryStore(
            ITextEmbeddingGeneration textEmbedding,
            ILogger logger)
        {
            _textEmbedding = textEmbedding;
            _logger = logger;
            _collectionName = "short-term";
            _hash = SHA1.Create();
        }

        public async Task Initialize(List<object> memories)
        {
            _memories = memories;

            if (!await DoesCollectionExistAsync(_collectionName))
                await CreateCollectionAsync(_collectionName);


            //Upsert the memories that were generated from faceted queries in Cog Search into SK volatile memory.
            //These were generated using ACSMemorySourceConfig.json in blob storage that was used to execute faceted queries in Cog Search to iterate through
            //each product category stored and count up the number of products in each category. The query also counts all the products for the entire company.
            //The content here has embeddings generated on it so it can be used in a vector query by the user.
            foreach (var memory in _memories)
            {
                var itemToEmbed = EmbeddingUtility.Transform(memory);
                var embbedding = await _textEmbedding.GenerateEmbeddingAsync(itemToEmbed.TextToEmbed);
                await UpsertAsync(
                    _collectionName,
                    new MemoryRecord(
                        new MemoryRecordMetadata(false, GetHash(itemToEmbed.TextToEmbed), null, null, null, JsonConvert.SerializeObject(memory)),
                        embbedding,
                        null)); ;
            }
        }

        private string GetHash(string s)
        {
            return Convert.ToBase64String(
                _hash.ComputeHash(
                    Encoding.UTF8.GetBytes(s)));
        }
    }
}
