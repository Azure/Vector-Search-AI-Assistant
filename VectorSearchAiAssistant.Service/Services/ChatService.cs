using VectorSearchAiAssistant.Service.Constants;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Chat;

namespace VectorSearchAiAssistant.Service.Services;

public class ChatService : IChatService
{
    /// <summary>
    /// All data is cached in the _sessions List object.
    /// </summary>
    private static List<Session> _sessions = new();

    private readonly ICosmosDbService _cosmosDbService;
    private readonly IOpenAiService _openAiService;
    private readonly IVectorDatabaseServiceQueries _vectorDatabaseService;
    private readonly int _maxConversationBytes;

    public ChatService(ICosmosDbService cosmosDbService, IOpenAiService openAiService,
        IVectorDatabaseServiceQueries vectorDatabaseService)
    {
        _cosmosDbService = cosmosDbService;
        _openAiService = openAiService;
        _vectorDatabaseService = vectorDatabaseService;

        _maxConversationBytes = openAiService.MaxConversationBytes;

    }

    /// <summary>
    /// Returns list of chat session ids and names for left-hand nav to bind to (display Name and ChatSessionId as hidden)
    /// </summary>
    public async Task<List<Session>> GetAllChatSessionsAsync()
    {
        return _sessions = await _cosmosDbService.GetSessionsAsync();
    }

    /// <summary>
    /// Returns the chat messages to display on the main web page when the user selects a chat from the left-hand nav
    /// </summary>
    public async Task<List<Message>> GetChatSessionMessagesAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        List<Message> chatMessages = new();

        if (_sessions.Count == 0)
        {
            return Enumerable.Empty<Message>().ToList();
        }

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        if (_sessions[index].Messages.Count == 0)
        {
            // Messages are not cached, go read from database
            chatMessages = await _cosmosDbService.GetSessionMessagesAsync(sessionId);

            // Cache results
            _sessions[index].Messages = chatMessages;
        }
        else
        {
            // Load from cache
            chatMessages = _sessions[index].Messages;
        }

        return chatMessages;
    }

    /// <summary>
    /// User creates a new Chat Session.
    /// </summary>
    public async Task CreateNewChatSessionAsync()
    {
        Session session = new();

        _sessions.Add(session);

        await _cosmosDbService.InsertSessionAsync(session);

    }

    /// <summary>
    /// Rename the Chat Ssssion from "New Chat" to the summary provided by OpenAI
    /// </summary>
    public async Task RenameChatSessionAsync(string? sessionId, string newChatSessionName)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        _sessions[index].Name = newChatSessionName;

        await _cosmosDbService.UpdateSessionAsync(_sessions[index]);
    }

    /// <summary>
    /// User deletes a chat session
    /// </summary>
    public async Task DeleteChatSessionAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        _sessions.RemoveAt(index);

        await _cosmosDbService.DeleteSessionAndMessagesAsync(sessionId);
    }

    /// <summary>
    /// Receive a prompt from a user, Vectorize it from _openAIService Get a completion from _openAiService
    /// </summary>
    public async Task<string> GetChatCompletionAsync(string? sessionId, string userPrompt)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        //Retrieve conversation, including latest prompt.
        //If you put this after the vector search it doesn't take advantage of previous information given so harder to chain prompts together.
        //However if you put this before the vector search it can get stuck on previous answers and not pull additional information. Worth experimenting
        //string conversation = GetChatSessionConversation(sessionId, userPrompt);


        //Get embeddings for user prompt.
        (float[] promptVectors, int vectorTokens) = await _openAiService.GetEmbeddingsAsync(userPrompt, sessionId);



        //Do vector search on prompt embeddings, return list of documents
        string retrievedDocuments = await _vectorDatabaseService.VectorSearchAsync(promptVectors);


        //Retrieve conversation, including latest prompt.
        string conversation = GetChatSessionConversation(sessionId, userPrompt);



        //Generate the completion to return to the user
        (string completion, int promptTokens, int responseTokens) = await _openAiService.GetChatCompletionAsync(sessionId, conversation, retrievedDocuments);


        //Add to prompt and completion to cache, then persist in Cosmos as transaction 
        Message promptMessage = new Message(sessionId, nameof(Participants.User), promptTokens, userPrompt);
        Message completionMessage = new Message(sessionId, nameof(Participants.Assistant), responseTokens, completion);        
        await AddPromptCompletionMessagesAsync(sessionId, promptMessage, completionMessage);


        return completion;
    }

    /// <summary>
    /// Get current conversation from newest to oldest up to max conversation tokens and add to the prompt
    /// </summary>
    private string GetChatSessionConversation(string sessionId, string userPrompt)
    {

        int? bytesUsed = 0;

        List<string> conversationBuilder = new List<string>();


        int index = _sessions.FindIndex(s => s.SessionId == sessionId);


        List<Message> messages = _sessions[index].Messages;

        //Start at the end of the list and work backwards
        for (int i = messages.Count - 1; i >= 0; i--)
        {

            bytesUsed += messages[i].Text.Length;

            if (bytesUsed > _maxConversationBytes)
                break;

            
            conversationBuilder.Add(messages[i].Text);

        }

        //Invert the chat messages to put back into chronological order and output as string.        
        string conversation = string.Join(Environment.NewLine, conversationBuilder.Reverse<string>());

        //Add the current userPrompt
        conversation += Environment.NewLine + userPrompt;

        return conversation;


    }

    public async Task<string> SummarizeChatSessionNameAsync(string? sessionId, string prompt)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        string response = await _openAiService.SummarizeAsync(sessionId, prompt);

        await RenameChatSessionAsync(sessionId, response);

        return response;
    }

    /// <summary>
    /// Add user prompt to the chat session message list object and insert into the data service.
    /// </summary>
    private async Task<Message> AddPromptMessageAsync(string sessionId, string promptText)
    {
        Message promptMessage = new(sessionId, nameof(Participants.User), default, promptText);

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        _sessions[index].AddMessage(promptMessage);

        return await _cosmosDbService.InsertMessageAsync(promptMessage);
    }


    /// <summary>
    /// Add user prompt and AI assistance response to the chat session message list object and insert into the data service as a transaction.
    /// </summary>
    private async Task AddPromptCompletionMessagesAsync(string sessionId, Message promptMessage, Message completionMessage)
    {

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);


        //Add prompt and completion to the cache
        _sessions[index].AddMessage(promptMessage);
        _sessions[index].AddMessage(completionMessage);


        //Update session cache with tokens used
        _sessions[index].TokensUsed += promptMessage.Tokens;
        _sessions[index].TokensUsed += completionMessage.Tokens;


        await _cosmosDbService.UpsertSessionBatchAsync(promptMessage, completionMessage, _sessions[index]);

    }
}