using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.Embeddings;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Service.Interfaces;
using Microsoft.Extensions.Logging;
using VectorSearchAiAssistant.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;
using VectorSearchAiAssistant.SemanticKernel.Skills.Core;
using VectorSearchAiAssistant.Service.Models.Search;
using Microsoft.SemanticKernel.Memory;

namespace VectorSearchAiAssistant.Service.Services;

public class SemanticKernelRAGService : IRAGService
{
    readonly SemanticKernelRAGServiceSettings _settings;
    readonly IKernel _semanticKernel;
    readonly ILogger<SemanticKernelRAGService> _logger;
    readonly ISystemPromptService _systemPromptService;
    readonly AzureCognitiveSearchVectorMemory _memory;

    bool _memoryInitialized = false;

    public int MaxConversationBytes => _settings.OpenAI.MaxConversationBytes;

    public bool IsInitialized => _memoryInitialized;

    public SemanticKernelRAGService(
        ISystemPromptService systemPromptService,
        IOptions<SemanticKernelRAGServiceSettings> options,
        ILogger<SemanticKernelRAGService> logger)
    {
        _systemPromptService = systemPromptService;
        _settings = options.Value;
        _logger = logger;

        var builder = new KernelBuilder();

        builder.WithLogger(_logger);

        builder.WithAzureTextEmbeddingGenerationService(
            _settings.OpenAI.EmbeddingsDeployment,
            _settings.OpenAI.Endpoint,
            _settings.OpenAI.Key);

        builder.WithAzureChatCompletionService(
            _settings.OpenAI.CompletionsDeployment,
            _settings.OpenAI.Endpoint,
            _settings.OpenAI.Key);

        _semanticKernel = builder.Build();

        _memory = new AzureCognitiveSearchVectorMemory(
            _settings.CognitiveSearch.Endpoint,
            _settings.CognitiveSearch.Key,
            _settings.CognitiveSearch.IndexName,
            _semanticKernel.GetService<ITextEmbeddingGeneration>(),
            _logger);
        InitializeMemory();

        _semanticKernel.RegisterMemory(_memory);
    }

    private async Task InitializeMemory()
    {
        await _memory.Initialize(new List<Type>
        {
            typeof(Customer),
            typeof(Product),
            typeof(SalesOrder)
        });

        _memoryInitialized = true;
    }

    public async Task<(string Completion, int UserPromptTokens, int ResponseTokens, float[]? UserPromptEmbedding)> GetResponse(string userPrompt)
    {
        var memorySkill = new TextEmbeddingObjectMemorySkill();
        //_semanticKernel.ImportSkill(memorySkill);
        var skContext = _semanticKernel.CreateNewContext();

        var memories = await memorySkill.RecallAsync(
            userPrompt,
            _settings.CognitiveSearch.IndexName,
            null,
            _settings.CognitiveSearch.MaxVectorSearchResults,
            skContext);
        // Read the resulting user prompt embedding as soon as possile
        var userPromptEmbedding = memorySkill.LastInputTextEmbedding?.ToArray();

        var chat = _semanticKernel.GetService<IChatCompletion>();

        var systemPrompt = _systemPromptService.GetPrompt(_settings.SystemPromptName);
        var chatHistory = chat.CreateNewChat($"{systemPrompt}{memories}");

        chatHistory.AddUserMessage(userPrompt);

        var reply = await chat.GenerateMessageAsync(chatHistory, new ChatRequestSettings());
        chatHistory.AddAssistantMessage(reply);

        return new(reply, 0, 0, userPromptEmbedding);
    }

    public async Task AddMemory<T>(T item, string itemName, Action<T, float[]> vectorizer)
    {
        await _memory.AddMemory<T>(item, itemName, vectorizer);
    }

    public async Task RemoveMemory<T>(T item)
    {
        await _memory.RemoveMemory<T>(item);
    }
}
