using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using VectorSearchAiAssistant.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.Embeddings;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace VectorSearchAiAssistant.Service.Services;

public class SemanticKernelRAGService : IRAGService
{
    readonly SemanticKernelRAGServiceSettings _settings;
    readonly IKernel _semanticKernel;
    readonly ILogger<SemanticKernelRAGService> _logger;
    readonly ISystemPromptService _systemPromptService;

    public int MaxConversationBytes => _settings.OpenAI.MaxConversationBytes;

    public SemanticKernelRAGService(
        ISystemPromptService systemPromptService,
        IOptions<SemanticKernelRAGServiceSettings> options,
        ILogger<SemanticKernelRAGService> logger)
    {
        _systemPromptService = systemPromptService;
        _settings = options.Value;
        _logger = logger;

        var builder = new KernelBuilder();

        builder.WithAzureTextEmbeddingGenerationService(
            _settings.OpenAI.EmbeddingsDeployment,
            _settings.OpenAI.Endpoint,
            _settings.OpenAI.Key);

        builder.WithAzureChatCompletionService(
            _settings.OpenAI.CompletionsDeployment,
            _settings.OpenAI.Endpoint,
            _settings.OpenAI.Key);

        _semanticKernel = builder.Build();

        _semanticKernel.RegisterMemory(new AzureCognitiveSearchVectorMemory(
            _settings.CognitiveSearch.Endpoint,
            _settings.CognitiveSearch.Key,
            _semanticKernel.GetService<ITextEmbeddingGeneration>()));
    }

    public async Task<(string Completion, int UserPromptTokens, int ResponseTokens, float[] UserPromptEmbedding)> GetResponse(string userPrompt)
    {
        var matchingMemories = await SearchMemoriesAsync(userPrompt);

        var chat = _semanticKernel.GetService<IChatCompletion>();

        var systemPrompt = _systemPromptService.GetPrompt(_settings.SystemPromptName);
        var chatHistory = chat.CreateNewChat($"{systemPrompt}{matchingMemories}");

        chatHistory.AddUserMessage(userPrompt);

        var reply = await chat.GenerateMessageAsync(chatHistory, new ChatRequestSettings());
        chatHistory.AddAssistantMessage(reply);

        return new(reply, 0, 0, Enumerable.Range(1, 10).Select(x => (float)x).ToArray());
    }

    private async Task<string> SearchMemoriesAsync(string query)
    {
        var retDocs = new List<string>();
        string resultDocuments = string.Empty;

        try
        {
            var searchResults = await _semanticKernel.Memory
                .SearchAsync(_settings.CognitiveSearch.IndexName, query, limit: _settings.CognitiveSearch.MaxVectorSearchResults, withEmbeddings: true)
                .ToListAsync();

            return string.Join(Environment.NewLine + "-",
                searchResults.Select(sr => sr.Metadata.AdditionalMetadata));
        }
        catch (Exception ex)
        {
            _logger.LogError($"There was an error conducting a memory search: {ex.Message}");
        }

        return resultDocuments;
    }
}
