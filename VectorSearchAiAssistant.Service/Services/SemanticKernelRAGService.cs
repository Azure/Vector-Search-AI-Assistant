using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.Embeddings;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Service.Interfaces;
using Microsoft.Extensions.Logging;
using VectorSearchAiAssistant.SemanticKernel.Skills.Core;
using VectorSearchAiAssistant.Service.Models.Search;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.AI.TextCompletion;
using VectorSearchAiAssistant.Service.Models.Chat;
using Newtonsoft.Json;
using VectorSearchAiAssistant.SemanticKernel.Chat;
using VectorSearchAiAssistant.SemanticKernel.Text;
using VectorSearchAiAssistant.Service.Models;
using VectorSearchAiAssistant.SemanticKernel.Memory.AzureCognitiveSearch;
using VectorSearchAiAssistant.SemanticKernel.MemorySource;
using Microsoft.SemanticKernel.Memory;
using VectorSearchAiAssistant.SemanticKernel.Memory;

namespace VectorSearchAiAssistant.Service.Services;

public class SemanticKernelRAGService : IRAGService
{
    readonly SemanticKernelRAGServiceSettings _settings;

    readonly IKernel _semanticKernel;
    readonly IEnumerable<IMemorySource> _memorySources;
    readonly ILogger<SemanticKernelRAGService> _logger;
    readonly ISystemPromptService _systemPromptService;
    readonly IChatCompletion _chat;
    readonly AzureCognitiveSearchVectorMemory _longTermMemory;
    readonly ShortTermVolatileMemoryStore _shortTermMemory;
    readonly Dictionary<string, Type> _memoryTypes;

    bool _memoryInitialized = false;

    public bool IsInitialized => _memoryInitialized;

    public SemanticKernelRAGService(
        ISystemPromptService systemPromptService,
        IEnumerable<IMemorySource> memorySources,
        IOptions<SemanticKernelRAGServiceSettings> options,
        IOptions<AzureCognitiveSearchMemorySourceSettings> cognitiveSearchMemorySourceSettings,
        ILogger<SemanticKernelRAGService> logger,
        ILogger<Kernel> skLogger)
    {
        _systemPromptService = systemPromptService;
        _memorySources = memorySources;
        _settings = options.Value;
        _logger = logger;

        _logger.LogInformation("Initializing the Semantic Kernel RAG service...");

        _memoryTypes = ModelRegistry.Models.ToDictionary(m => m.Key, m => m.Value.Type);

        var builder = new KernelBuilder();

        builder.WithLogger(skLogger);

        builder.WithAzureTextEmbeddingGenerationService(
            _settings.OpenAI.EmbeddingsDeployment,
            _settings.OpenAI.Endpoint,
            _settings.OpenAI.Key);

        builder.WithAzureChatCompletionService(
            _settings.OpenAI.CompletionsDeployment,
            _settings.OpenAI.Endpoint,
            _settings.OpenAI.Key);

        _semanticKernel = builder.Build();

        _longTermMemory = new AzureCognitiveSearchVectorMemory(
            _settings.CognitiveSearch.Endpoint,
            _settings.CognitiveSearch.Key,
            _settings.CognitiveSearch.IndexName,
            _semanticKernel.GetService<ITextEmbeddingGeneration>(),
            _logger);

        _shortTermMemory = new ShortTermVolatileMemoryStore(
            _semanticKernel.GetService<ITextEmbeddingGeneration>(),
            _logger);

        Task.Run(() =>  InitializeMemory());

        _semanticKernel.RegisterMemory(_longTermMemory);

        _chat = _semanticKernel.GetService<IChatCompletion>();

        _logger.LogInformation("Semantic Kernel RAG service initialized.");
    }

    private async Task InitializeMemory()
    {
        try
        {
            _logger.LogInformation("Initializing the Semantic Kernel RAG service memory...");
            await _longTermMemory.Initialize(_memoryTypes.Values.ToList());

            // Get current short term memories
            // TODO: Explore the option of moving static memories loaded from blob storage into the long-term memory (e.g., the Azure Cognitive Search index).
            // For now, the static memories are re-loaded each time together with the analytical short-term memories originating from Azure Cognitive Search faceted queries.
            var shortTermMemories = new List<string>();
            foreach (var memorySource in _memorySources)
            {
                shortTermMemories.AddRange(await memorySource.GetMemories());
            }

            await _shortTermMemory.Initialize(shortTermMemories
                .Select(m => (object)new ShortTermMemory
                {
                    entityType__ = nameof(ShortTermMemory),
                    memory__ = m
                }).ToList());

            _memoryInitialized = true;
            _logger.LogInformation("Semantic Kernel RAG service memory initialized.");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The Semantic Kernel RAG service memory failed to initialize.");
        }
    }

    public async Task<(string Completion, string UserPrompt, int UserPromptTokens, int ResponseTokens, float[]? UserPromptEmbedding)> GetResponse(string userPrompt, List<Message> messageHistory)
    {
        var memorySkill = new TextEmbeddingObjectMemorySkill(
            _longTermMemory,
            _shortTermMemory,
            _logger);

        var memories = await memorySkill.RecallAsync(
            userPrompt,
            _settings.CognitiveSearch.IndexName,
            0.8,
            _settings.CognitiveSearch.MaxVectorSearchResults);

        // Read the resulting user prompt embedding as soon as possible
        var userPromptEmbedding = memorySkill.LastInputTextEmbedding?.ToArray();

        List<string> memoryCollection;
        if (string.IsNullOrEmpty(memories))
            memoryCollection = new List<string>();
        else
            memoryCollection = JsonConvert.DeserializeObject<List<string>>(memories);

        var chatHistory = new ChatBuilder(
                _semanticKernel,
                _settings.OpenAI.CompletionsDeploymentMaxTokens,
                _memoryTypes,
                promptOptimizationSettings: _settings.OpenAI.PromptOptimization)
            .WithSystemPrompt(
                await _systemPromptService.GetPrompt(_settings.OpenAI.ChatCompletionPromptName))
            .WithMemories(
                memoryCollection)
            .WithMessageHistory(
                messageHistory.Select(m => (new AuthorRole(m.Sender.ToLower()), m.Text.NormalizeLineEndings())).ToList())
            .Build();

        chatHistory.AddUserMessage(userPrompt);

        var chat = _semanticKernel.GetService<IChatCompletion>();
        var completionResults = await chat.GetChatCompletionsAsync(chatHistory);

        // TODO: Add validation and perhaps fall back to a standard response if no completions are generated.
        var reply = await completionResults[0].GetChatMessageAsync();
        var rawResult = (completionResults[0] as ITextResult).ModelResult.GetOpenAIChatResult();

        return new(reply.Content, chatHistory[0].Content, rawResult.Usage.PromptTokens, rawResult.Usage.CompletionTokens, userPromptEmbedding);
    }

    public async Task<string> Summarize(string sessionId, string userPrompt)
    {
        var summarizerSkill = new GenericSummarizerSkill(
            await _systemPromptService.GetPrompt(_settings.OpenAI.ShortSummaryPromptName),
            500,
            _semanticKernel);

        var updatedContext = await summarizerSkill.SummarizeConversationAsync(
            userPrompt,
            _semanticKernel.CreateNewContext());

        //Remove all non-alpha numeric characters (Turbo has a habit of putting things in quotes even when you tell it not to
        var summary = Regex.Replace(updatedContext.Result, @"[^a-zA-Z0-9\s]", "");

        return summary;
    }

    public async Task AddMemory(object item, string itemName, Action<object, float[]> vectorizer)
    {
        if (item is EmbeddedEntity entity)
            entity.entityType__ = item.GetType().Name;
        else
            throw new ArgumentException("Only objects derived from EmbeddedEntity can be added to memory.");

        await _longTermMemory.AddMemory(item, itemName, vectorizer);
    }

    public async Task RemoveMemory(object item)
    {
        await _longTermMemory.RemoveMemory(item);
    }
}
