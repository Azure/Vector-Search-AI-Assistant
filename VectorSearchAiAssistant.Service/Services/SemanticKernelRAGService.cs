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
using System.Linq;
using Microsoft.SemanticKernel.AI.TextCompletion;
using VectorSearchAiAssistant.Service.Models.Chat;
using Newtonsoft.Json;
using VectorSearchAiAssistant.SemanticKernel.Chat;
using VectorSearchAiAssistant.SemanticKernel.Text;
using VectorSearchAiAssistant.Service.Models;
using VectorSearchAiAssistant.SemanticKernel.Memory.AzureCognitiveSearch;

namespace VectorSearchAiAssistant.Service.Services;

public class SemanticKernelRAGService : IRAGService
{
    readonly SemanticKernelRAGServiceSettings _settings;
    readonly IKernel _semanticKernel;
    readonly ILogger<SemanticKernelRAGService> _logger;
    readonly ISystemPromptService _systemPromptService;
    readonly IChatCompletion _chat;
    readonly AzureCognitiveSearchVectorMemory _memory;
    readonly Dictionary<string, Type> _memoryTypes;

    bool _memoryInitialized = false;

    public bool IsInitialized => _memoryInitialized;

    public SemanticKernelRAGService(
        ISystemPromptService systemPromptService,
        IOptions<SemanticKernelRAGServiceSettings> options,
        ILogger<SemanticKernelRAGService> logger)
    {
        _systemPromptService = systemPromptService;
        _settings = options.Value;
        _logger = logger;

        _memoryTypes = ModelRegistry.Models.ToDictionary(m => m.Key, m => m.Value.Type);

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
        Task.Run(() =>  InitializeMemory());

        _semanticKernel.RegisterMemory(_memory);

        _chat = _semanticKernel.GetService<IChatCompletion>();
    }

    private async Task InitializeMemory()
    {
        await _memory.Initialize(_memoryTypes.Values.ToList());

        _memoryInitialized = true;
    }

    public async Task<(string Completion, int UserPromptTokens, int ResponseTokens, float[]? UserPromptEmbedding)> GetResponse(string userPrompt, List<Message> messageHistory)
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

        List<string> memoryCollection;
        if (string.IsNullOrEmpty(memories))
            memoryCollection = new List<string>();
        else
        {
            memoryCollection = JsonConvert.DeserializeObject<List<string>>(memories);
        }

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
                messageHistory.Select(m => (new AuthorRole(m.Sender), m.Text.NormalizeLineEndings())).ToList())
            .Build();

        chatHistory.AddUserMessage(userPrompt);

        var chat = _semanticKernel.GetService<IChatCompletion>();
        var completionResults = await chat.GetChatCompletionsAsync(chatHistory);

        // TODO: Add validation and perhaps fall back to a standard response if no completions are generated.
        var reply = await completionResults[0].GetChatMessageAsync();
        var rawResult = (completionResults[0] as ITextResult).ModelResult.GetOpenAIChatResult();

        return new(reply.Content, rawResult.Usage.PromptTokens, rawResult.Usage.CompletionTokens, userPromptEmbedding);
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

        await _memory.AddMemory(item, itemName, vectorizer);
    }

    public async Task RemoveMemory(object item)
    {
        await _memory.RemoveMemory(item);
    }
}
