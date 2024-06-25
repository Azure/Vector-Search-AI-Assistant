using BuildYourOwnCopilot.Common.Models.Chat;
using BuildYourOwnCopilot.Infrastructure.Interfaces;
using BuildYourOwnCopilot.Infrastructure.Models.ConfigurationOptions;
using BuildYourOwnCopilot.SemanticKernel.Memory;
using BuildYourOwnCopilot.SemanticKernel.Models;
using BuildYourOwnCopilot.SemanticKernel.Plugins.Memory;
using BuildYourOwnCopilot.Service.Constants;
using BuildYourOwnCopilot.Service.Interfaces;
using BuildYourOwnCopilot.Service.Models.Chat;
using BuildYourOwnCopilot.Service.Models.ConfigurationOptions;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;

#pragma warning disable SKEXP0010, SKEXP0020;

namespace BuildYourOwnCopilot.Infrastructure.Services
{
    public class SemanticCacheService : ISemanticCacheService
    {
        private readonly SemanticCacheServiceSettings _settings;
        private readonly CosmosDBVectorStoreSettings _searchSettings;
        private readonly VectorMemoryStore _memoryStore;
        private readonly ITokenizerService _tokenizer;
        private readonly string _tokenizerEncoder;
        private readonly ILogger<SemanticCacheService> _logger;

        public SemanticCacheService(
            SemanticCacheServiceSettings settings,
            OpenAISettings openAISettings,
            CosmosDBVectorStoreSettings searchSettings,
            ICosmosDBClientFactory cosmosDBClientFactory,
            ITokenizerService tokenizerService,
            string tokenizerEncoder,
            ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _searchSettings = searchSettings;
            _memoryStore = new VectorMemoryStore(
                _searchSettings.IndexName,
                new AzureCosmosDBNoSQLMemoryStore(
                    cosmosDBClientFactory.Client,
                    cosmosDBClientFactory.DatabaseName,
                    searchSettings.VectorEmbeddingPolicy,
                    searchSettings.IndexingPolicy
                ),
                new AzureOpenAITextEmbeddingGenerationService(
                    openAISettings.EmbeddingsDeployment,
                    openAISettings.Endpoint,
                    openAISettings.Key,
                    dimensions: (int)_searchSettings.Dimensions
                ),
                loggerFactory.CreateLogger<VectorMemoryStore>());
            _tokenizer = tokenizerService;
            _tokenizerEncoder = tokenizerEncoder;

            _logger = loggerFactory.CreateLogger<SemanticCacheService>();
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

            try
            {

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cache search: {ErrorMessage}.", ex.Message);
            }

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
