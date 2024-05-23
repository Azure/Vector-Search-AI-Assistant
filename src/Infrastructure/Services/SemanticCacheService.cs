using MathNet.Numerics;
using Newtonsoft.Json;
using VectorSearchAiAssistant.Common.Models.Chat;
using VectorSearchAiAssistant.Common.Models.ConfigurationOptions;
using VectorSearchAiAssistant.SemanticKernel.Plugins.Memory;
using VectorSearchAiAssistant.Service.Constants;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Chat;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;

namespace VectorSearchAiAssistant.Service.Services
{
    public class SemanticCacheService : ISemanticCacheService
    {
        private readonly SemanticCacheServiceSettings _settings;
        private readonly VectorSearchSettings _searchSettings;
        private readonly VectorMemoryStore _memoryStore;
        private readonly ITokenizerService _tokenizer;
        private readonly string _tokenizerEncoder;

        public SemanticCacheService(
            SemanticCacheServiceSettings semanticCacheServiceSettings,
            VectorSearchSettings searchSettings,
            VectorMemoryStore memoryStore,
            ITokenizerService tokenizerService,
            string tokenizerEncoder)
        {
            _settings = semanticCacheServiceSettings;
            _searchSettings = searchSettings;
            _memoryStore = memoryStore;
            _tokenizer = tokenizerService;
            _tokenizerEncoder = tokenizerEncoder;
        }

        public async Task Initialize() =>
            await _memoryStore.Initialize();

        public async Task<SemanticCacheItem> GetCacheItem(string userPrompt, List<Message> messageHistory)
        {
            var uniqueId = Guid.NewGuid().ToString().ToLower();
            var cacheItem = new SemanticCacheItem()
            {
                Id = uniqueId,
                PartitionKey = uniqueId,
                UserPrompt = userPrompt,
                UserPromptEmbedding = await _memoryStore.GetEmbedding(userPrompt),
                UserPromptTokens = _tokenizer.Encode(userPrompt, _tokenizerEncoder).Count
            };
            var userMessageHistory = messageHistory.Where(m => m.Sender == nameof(Participants.User)).ToList();
            var assistantMessageHistory = messageHistory.Where(m => m.Sender == nameof(Participants.Assistant)).ToList();

            if (userMessageHistory.Count > 0)
            {
                var similarity = 1 - Distance.Cosine(cacheItem.UserPromptEmbedding.ToArray(), userMessageHistory.Last().Vector!);
                if (similarity >= _searchSettings.MinRelevance)
                {
                    // Looks like the user just repeated the previous question
                    cacheItem.ConversationContext = userMessageHistory.Last().Text;
                    cacheItem.ConversationContextTokens = userMessageHistory.Last().TokensSize!.Value;
                    cacheItem.Completion = assistantMessageHistory.Last().Text;
                    cacheItem.CompletionTokens = assistantMessageHistory.Last().TokensSize!.Value;

                    return cacheItem;
                }
            }

            await SetConversationContext(cacheItem, userMessageHistory);

            var cacheMatches = await _memoryStore
                .GetNearestMatches(
                    cacheItem.ConversationContextEmbedding,
                    1,
                    _searchSettings.MinRelevance)
                .ToListAsync()
                .ConfigureAwait(false);
            if (cacheMatches.Count == 0)
                return cacheItem;

            var matchedCacheItem = JsonConvert.DeserializeObject<SemanticCacheItem>(
                cacheMatches.First().Metadata.AdditionalMetadata);

            cacheItem.Completion = matchedCacheItem!.Completion;
            cacheItem.CompletionTokens = matchedCacheItem.CompletionTokens;

            return cacheItem;
        }

        public async Task SetCacheItem(SemanticCacheItem cacheItem) =>
            await _memoryStore.AddMemory(
                cacheItem.Id,
                cacheItem.ConversationContext,
                cacheItem.ConversationContextEmbedding,
                JsonConvert.SerializeObject(cacheItem),
                cacheItem.PartitionKey);

        private async Task SetConversationContext(SemanticCacheItem cacheItem, List<Message> userMessageHistory)
        {
            var tokensCount = cacheItem.UserPromptTokens;
            var result = new List<string> { cacheItem.UserPrompt };

            for (int i = userMessageHistory.Count - 1; i >= 0; i--)
            {
                tokensCount += userMessageHistory[i].TokensSize!.Value;
                if (tokensCount > _settings.ConversationContextMaxTokens)
                    break;
                result.Insert(0, userMessageHistory[i].Text);
            }

            cacheItem.ConversationContext = string.Join(Environment.NewLine, [.. result]);
            cacheItem.ConversationContextTokens = _tokenizer.Encode(cacheItem.ConversationContext, _tokenizerEncoder).Count;
            cacheItem.ConversationContextEmbedding = await _memoryStore.GetEmbedding(cacheItem.ConversationContext);
        }
    }
}
