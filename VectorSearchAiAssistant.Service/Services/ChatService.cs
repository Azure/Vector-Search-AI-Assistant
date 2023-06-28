using Microsoft.Extensions.Logging;
using VectorSearchAiAssistant.Service.Constants;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Chat;
using VectorSearchAiAssistant.Service.Models.Search;

namespace VectorSearchAiAssistant.Service.Services;

public class ChatService : IChatService
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IRAGService _ragService;
    private readonly ILogger _logger;

    public bool IsInitialized => _cosmosDbService.IsInitialized && _ragService.IsInitialized;

    public ChatService(
        ICosmosDbService cosmosDbService,
        IRAGService ragService,
        ILogger<ChatService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// Returns list of chat session ids and names.
    /// </summary>
    public async Task<List<Session>> GetAllChatSessionsAsync()
    {
        return await _cosmosDbService.GetSessionsAsync();
    }

    /// <summary>
    /// Returns the chat messages related to an existing session.
    /// </summary>
    public async Task<List<Message>> GetChatSessionMessagesAsync(string sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        return await _cosmosDbService.GetSessionMessagesAsync(sessionId);
    }

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    public async Task<Session> CreateNewChatSessionAsync()
    {
        Session session = new();
        return await _cosmosDbService.InsertSessionAsync(session);
    }

    /// <summary>
    /// Rename the chat session from its default (eg., "New Chat") to the summary provided by OpenAI.
    /// </summary>
    public async Task<Session> RenameChatSessionAsync(string sessionId, string newChatSessionName)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentException.ThrowIfNullOrEmpty(newChatSessionName);

        return await _cosmosDbService.UpdateSessionNameAsync(sessionId, newChatSessionName);
    }

    /// <summary>
    /// Delete a chat session and related messages.
    /// </summary>
    public async Task DeleteChatSessionAsync(string sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        await _cosmosDbService.DeleteSessionAndMessagesAsync(sessionId);
    }

    /// <summary>
    /// Receive a prompt from a user, vectorize it from the OpenAI service, and get a completion from the OpenAI service.
    /// </summary>
    public async Task<Completion> GetChatCompletionAsync(string? sessionId, string userPrompt)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        // Retrieve conversation, including latest prompt.
        // If you put this after the vector search it doesn't take advantage of previous information given so harder to chain prompts together.
        // However if you put this before the vector search it can get stuck on previous answers and not pull additional information. Worth experimenting

        // Retrieve conversation, including latest prompt.
        var messages = await _cosmosDbService.GetSessionMessagesAsync(sessionId);

        // Generate the completion to return to the user
        //(string completion, int promptTokens, int responseTokens) = await_openAiService.GetChatCompletionAs ync(sessionId, conversation, retrievedDocuments);
        var result = await _ragService.GetResponse(userPrompt, messages);

        // Add to prompt and completion to cache, then persist in Cosmos as transaction 
        var promptMessage = new Message(sessionId, nameof(Participants.User), result.UserPromptTokens, userPrompt, result.UserPromptEmbedding, null);
        var completionMessage = new Message(sessionId, nameof(Participants.Assistant), result.ResponseTokens, result.Completion, null, null);        
        await AddPromptCompletionMessagesAsync(sessionId, promptMessage, completionMessage);

        return new Completion { Text = result.Completion };
    }

    /// <summary>
    /// Generate a name for a chat message, based on the passed in prompt.
    /// </summary>
    public async Task<Completion> SummarizeChatSessionNameAsync(string? sessionId, string prompt)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        await Task.CompletedTask;

        var summary = await _ragService.Summarize(sessionId, prompt);

        await RenameChatSessionAsync(sessionId, summary);

        return new Completion { Text = summary };
    }

    /// <summary>
    /// Add a new user prompt to the chat session and insert into the data service.
    /// </summary>
    private async Task<Message> AddPromptMessageAsync(string sessionId, string promptText)
    {
        Message promptMessage = new(sessionId, nameof(Participants.User), default, promptText, null, null);

        return await _cosmosDbService.InsertMessageAsync(promptMessage);
    }


    /// <summary>
    /// Add user prompt and AI assistance response to the chat session message list object and insert into the data service as a transaction.
    /// </summary>
    private async Task AddPromptCompletionMessagesAsync(string sessionId, Message promptMessage, Message completionMessage)
    {
        var session = await _cosmosDbService.GetSessionAsync(sessionId);

        // Update session cache with tokens used
        session.TokensUsed += promptMessage.Tokens;
        session.TokensUsed += completionMessage.Tokens;

        await _cosmosDbService.UpsertSessionBatchAsync(promptMessage, completionMessage,session);
    }

    /// <summary>
    /// Rate an assistant message. This can be used to discover useful AI responses for training, discoverability, and other benefits down the road.
    /// </summary>
    public async Task<Message> RateMessageAsync(string id, string sessionId, bool? rating)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(sessionId);

        return await _cosmosDbService.UpdateMessageRatingAsync(id, sessionId, rating);
    }

    public async Task AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNullOrEmpty(product.id);
        ArgumentNullException.ThrowIfNullOrEmpty(product.categoryId);

        await _cosmosDbService.InsertProductAsync(product);
    }

    public async Task DeleteProduct(string productId, string categoryId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(productId);
        ArgumentNullException.ThrowIfNullOrEmpty(categoryId);

        await _cosmosDbService.DeleteProductAsync(productId, categoryId);

        try
        {
            await _ragService.RemoveMemory<Product>(new Product { id = productId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error attempting to remove memory for product id {productId} (category id {categoryId})");
        }
    }
}