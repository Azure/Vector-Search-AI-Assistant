using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using System.Text.RegularExpressions;
using BuildYourOwnCopilot.Common.Interfaces;
using BuildYourOwnCopilot.Common.Models.BusinessDomain;
using BuildYourOwnCopilot.Common.Models.Chat;
using BuildYourOwnCopilot.SemanticKernel.Connectors.AzureCosmosDBNoSql;
using BuildYourOwnCopilot.SemanticKernel.Plugins.Core;
using BuildYourOwnCopilot.SemanticKernel.Plugins.Memory;
using BuildYourOwnCopilot.Service.Interfaces;
using BuildYourOwnCopilot.Service.Models.ConfigurationOptions;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050, SKEXP0060

namespace BuildYourOwnCopilot.Service.Services;

public class SemanticKernelRAGService : IRAGService
{
    readonly SemanticKernelRAGServiceSettings _settings;
    readonly Kernel _semanticKernel;
    readonly IEnumerable<IMemorySource> _memorySources;
    readonly ILogger<SemanticKernelRAGService> _logger;
    readonly ISystemPromptService _systemPromptService;
    readonly ICosmosDBVectorStoreService _cosmosDBVectorStoreService;
    readonly ITokenizerService _tokenizerService;
    readonly VectorMemoryStore _longTermMemory;
    readonly VectorMemoryStore _shortTermMemory;
    readonly ISemanticCacheService _semanticCache;

    readonly IItemTransformerFactory _itemTransformerFactory;

    readonly string _shortTermCollectionName = "short-term";

    bool _serviceInitialized = false;
    bool _shortTermMemoryInitialized = false;
    bool _semanticMemoryInitialized = false;

    string _prompt = string.Empty;

    public bool IsInitialized => _serviceInitialized;

    public SemanticKernelRAGService(
        IItemTransformerFactory itemTransformerFactory,
        ISystemPromptService systemPromptService,
        IEnumerable<IMemorySource> memorySources,
        ICosmosDBVectorStoreService cosmosDBVectorStoreService,
        ITokenizerService tokenizerService,
        IOptions<SemanticKernelRAGServiceSettings> options,
        ILogger<SemanticKernelRAGService> logger,
        ILoggerFactory loggerFactory)
    {
        _itemTransformerFactory = itemTransformerFactory;
        _systemPromptService = systemPromptService;
        _cosmosDBVectorStoreService = cosmosDBVectorStoreService;
        _tokenizerService = tokenizerService;
        _memorySources = memorySources;
        _settings = options.Value;
        _logger = logger;

        _logger.LogInformation("Initializing the Semantic Kernel RAG service...");

        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton<ILoggerFactory>(loggerFactory);

        builder.AddAzureOpenAITextEmbeddingGeneration(
            _settings.OpenAI.EmbeddingsDeployment,
            _settings.OpenAI.Endpoint,
            _settings.OpenAI.Key);

        builder.AddAzureOpenAIChatCompletion(
            _settings.OpenAI.CompletionsDeployment,
            _settings.OpenAI.Endpoint,
            _settings.OpenAI.Key);

        _semanticKernel = builder.Build();

        // The long-term memory uses an Azure Cosmos DB NoSQL memory store
        _longTermMemory = new VectorMemoryStore(
            _settings.KnowledgeRetrieval.IndexName,
            new AzureCosmosDBNoSqlMemoryStore(_cosmosDBVectorStoreService),
            _semanticKernel.Services.GetRequiredService<ITextEmbeddingGenerationService>()!,
            loggerFactory.CreateLogger<VectorMemoryStore>());

        _shortTermMemory = new VectorMemoryStore(
            _shortTermCollectionName,
            new VolatileMemoryStore(),
            _semanticKernel.Services.GetRequiredService<ITextEmbeddingGenerationService>()!,
            loggerFactory.CreateLogger<VectorMemoryStore>());

        _semanticCache = new SemanticCacheService(
            _settings.SemanticCache,
            _settings.SemanticCacheRetrieval,
            new VectorMemoryStore(
                _settings.SemanticCacheRetrieval.IndexName,
                new AzureCosmosDBNoSqlMemoryStore(_cosmosDBVectorStoreService),
                _semanticKernel.Services.GetRequiredService<ITextEmbeddingGenerationService>()!,
                loggerFactory.CreateLogger<VectorMemoryStore>()),
            _tokenizerService,
            _settings.TextSplitter.TokenizerEncoder!);

        Task.Run(Initialize);
    }

    private async Task Initialize()
    {
        await _longTermMemory.Initialize();
        await _semanticCache.Initialize();

        _prompt = await _systemPromptService.GetPrompt(_settings.OpenAI.ChatCompletionPromptName);

        var kmContextPlugin = new KnowledgeManagementContextPlugin(
            _longTermMemory,
            _shortTermMemory,
            _prompt,
            _settings.KnowledgeRetrieval,
            _settings.OpenAI,
            _logger);

        _semanticKernel.ImportPluginFromObject(kmContextPlugin);

        _serviceInitialized = true;

        _logger.LogInformation("Semantic Kernel RAG service initialized.");
    }

    private async Task EnsureShortTermMemory()
    {
        try
        {
            if (_shortTermMemoryInitialized)
                return;

            // The memories collection in the short term memory store must be created explicitly
            await _shortTermMemory.MemoryStore.CreateCollectionAsync(_shortTermCollectionName);

            // Get current short term memories. Short term memories are generated or loaded at runtime and kept in SK's volatile memory.
            //The content here has embeddings generated on it so it can be used in a vector query by the user.

            // TODO: Explore the option of moving static memories loaded from blob storage into the long-term memory (e.g., the Azure Cosmos DB vector store collection).
            // For now, the static memories are re-loaded each time.
            var shortTermMemories = new List<string>();
            foreach (var memorySource in _memorySources)
            {
                shortTermMemories.AddRange(await memorySource.GetMemories());
            }

            foreach (var itemTransformer in shortTermMemories
                .Select(m => _itemTransformerFactory.CreateItemTransformer(new ShortTermMemory
                {
                    entityType__ = nameof(ShortTermMemory),
                    memory__ = m
                })))
            {
                await _shortTermMemory.AddMemory(itemTransformer);
            }

            _shortTermMemoryInitialized = true;
            _logger.LogInformation("Semantic Kernel RAG service short-term memory initialized.");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The Semantic Kernel RAG service short-term memory failed to initialize.");
        }
    }

    public async Task<CompletionResult> GetResponse(string userPrompt, List<Message> messageHistory)
    {
        var cacheItem = await _semanticCache.GetCacheItem(userPrompt, messageHistory);
        if (!string.IsNullOrEmpty(cacheItem.Completion))
            // If the Completion property is set, it means the cache item was populated with a hit from the cache
            return new CompletionResult
            {
                UserPrompt = userPrompt,
                UserPromptTokens = cacheItem.UserPromptTokens,
                UserPromptEmbedding = cacheItem.UserPromptEmbedding.ToArray(),
                RenderedPrompt = cacheItem.ConversationContext,
                RenderedPromptTokens = cacheItem.ConversationContextTokens,
                Completion = cacheItem.Completion,
                CompletionTokens = cacheItem.CompletionTokens,
                FromCache = true
            };

        // The semantic cache was not able to retrieve a hit from the cache so we are moving on with the normal flow.
        // We still need to keep the cache item around as it contains the properties we need later on to update the cache with the new entry.

        await EnsureShortTermMemory();

        // Use observability features to capture the fully rendered prompt.
        var promptFilter = new DefaultPromptFilter();
        _semanticKernel.PromptFilters.Add(promptFilter);
        
        var arguments = new KernelArguments()
        {
            ["userPrompt"] = userPrompt,
            ["messageHistory"] = messageHistory
        };

        var result = await _semanticKernel.InvokePromptAsync(_prompt, arguments);

        var completion = result.GetValue<string>()!;
        var completionUsage = (result.Metadata!["Usage"] as CompletionsUsage)!;

        // Add the completion to the semantic memory
        cacheItem.Completion = completion;
        cacheItem.CompletionTokens = completionUsage!.CompletionTokens;
        await _semanticCache.SetCacheItem(cacheItem);

        return new CompletionResult
        {
            UserPrompt = userPrompt,
            UserPromptTokens = cacheItem.UserPromptTokens,
            UserPromptEmbedding = cacheItem.UserPromptEmbedding.ToArray(),
            RenderedPrompt = promptFilter.RenderedPrompt,
            RenderedPromptTokens = completionUsage.PromptTokens,
            Completion = completion,
            CompletionTokens = completionUsage.CompletionTokens,
            FromCache = false
        };
    }

    public async Task<string> Summarize(string sessionId, string userPrompt)
    {
        var summarizerPlugin = new TextSummaryPlugin(
            await _systemPromptService.GetPrompt(_settings.OpenAI.ShortSummaryPromptName),
            500,
            _semanticKernel);

        var updatedContext = await summarizerPlugin.SummarizeTextAsync(
            userPrompt);

        //Remove all non-alpha numeric characters (Turbo has a habit of putting things in quotes even when you tell it not to)
        var summary = Regex.Replace(updatedContext, @"[^a-zA-Z0-9.\s]", "");

        return summary;
    }

    public async Task AddMemory(IItemTransformer itemTransformer)
    {
        await _longTermMemory.AddMemory(itemTransformer);
    }

    public async Task RemoveMemory(IItemTransformer itemTransformer)
    {
        await _longTermMemory.RemoveMemory(itemTransformer);
    }
}
