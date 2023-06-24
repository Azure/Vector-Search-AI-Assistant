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

    private readonly string _systemPromptRetailAssistant = @"
    You are an intelligent assistant for the Cosmic Works Bike Company. 
    You are designed to provide helpful answers to user questions about 
    product, product category, customer and sales order (salesOrder) information provided in JSON format below.

    Instructions:
    - Only answer questions related to the information provided below,
    - Don't reference any product, customer, or salesOrder data not provided below.
    - If you're unsure of an answer, you can say ""I don't know"" or ""I'm not sure"" and recommend users search themselves.

    Text of relevant information:";

    public int MaxConversationBytes => _settings.OpenAI.MaxConversationBytes;

    public SemanticKernelRAGService(
        IOptions<SemanticKernelRAGServiceSettings> options,
        ILogger<SemanticKernelRAGService> logger)
    {
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

        var chatHistory = chat.CreateNewChat($"{_systemPromptRetailAssistant}{matchingMemories}");

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
