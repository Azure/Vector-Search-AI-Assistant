using Search.Constants;
using Search.Models;
using SharpToken;

namespace Search.Services;

public class ChatService
{
    /// <summary>
    /// All data is cached in the _sessions List object.
    /// </summary>
    private static List<Session> _sessions = new();

    private readonly OpenAiService _openAiService;
    private readonly MongoDbService _mongoDbService;
    private readonly int _maxConversationTokens;
    private readonly int _maxCompletionTokens;
    private readonly ILogger _logger;

    public ChatService(OpenAiService openAiService, MongoDbService mongoDbService, ILogger logger)
    {

        _openAiService = openAiService;
        _mongoDbService = mongoDbService;

        _maxConversationTokens = openAiService.MaxConversationTokens;
        _maxCompletionTokens = openAiService.MaxCompletionTokens;
        _logger = logger;
    }

    /// <summary>
    /// Returns list of chat session ids and names for left-hand nav to bind to (display Name and ChatSessionId as hidden)
    /// </summary>
    public async Task<List<Session>> GetAllChatSessionsAsync()
    {  
        return _sessions = await _mongoDbService.GetSessionsAsync();
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
            chatMessages = await _mongoDbService.GetSessionMessagesAsync(sessionId);

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

        await _mongoDbService.InsertSessionAsync(session);

    }

    /// <summary>
    /// Rename the Chat Ssssion from "New Chat" to the summary provided by OpenAI
    /// </summary>
    public async Task RenameChatSessionAsync(string? sessionId, string newChatSessionName)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        _sessions[index].Name = newChatSessionName;

        await _mongoDbService.UpdateSessionAsync(_sessions[index]);
    }

    /// <summary>
    /// User deletes a chat session
    /// </summary>
    public async Task DeleteChatSessionAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        _sessions.RemoveAt(index);

        await _mongoDbService.DeleteSessionAndMessagesAsync(sessionId);
    }

    /// <summary>
    /// Receive a prompt from a user, Vectorize it from _openAIService Get a completion from _openAiService
    /// </summary>
    public async Task<string> GetChatCompletionAsync(string? sessionId, string userPrompt)
    {

        try
        { 
            ArgumentNullException.ThrowIfNull(sessionId);

            //Retrieve conversation, including latest prompt.
            //If you put this after the vector search it doesn't take advantage of previous information given so harder to chain prompts together.
            //However if you put this before the vector search it can get stuck on previous answers and not pull additional information. Worth experimenting
            
            (string conversationAndUserPrompt, int conversationTokens) = GetChatSessionConversation(sessionId, userPrompt);


            //Get embeddings for user prompt.
            (float[] promptVectors, int vectorTokens) = await _openAiService.GetEmbeddingsAsync(sessionId, conversationAndUserPrompt);


            //Do vector search on prompt embeddings, return list of documents
            string retrievedDocuments = await _mongoDbService.VectorSearchAsync(promptVectors);


            //Estimate token usage and trim vector data sent to OpenAI to prevent exceptions caused by exceeding token limits.
            (string augmentedContent, int newUserPromptTokens) = BuildPromptAndData(sessionId, userPrompt, conversationTokens, retrievedDocuments);


            //Generate the completion to return to the user
            (string completion, int promptTokens, int completionTokens) = await _openAiService.GetChatCompletionAsync(sessionId, conversationAndUserPrompt, augmentedContent);


            //Add to prompt and completion to cache, then persist in Cosmos as transaction
            Message promptMessage = new Message(sessionId, nameof(Participants.User), newUserPromptTokens, default, userPrompt);
            Message completionMessage = new Message(sessionId, nameof(Participants.Assistant), completionTokens, promptTokens, completion);        
            await AddPromptCompletionMessagesAsync(sessionId, promptMessage, completionMessage);


            return completion;

        }
        catch (Exception ex) 
        {
            string message = $"ChatService.GetChatCompletionAsync(): {ex.Message}";
            _logger.LogError(message);
            throw;

        }
    }

    /// <summary>
    /// Estimate the token usage for OpenAI completion to prevent exceeding the OpenAI model's maximum token limit. This function estimates the
    /// amount of tokens the vector search result data and the user prompt will consume. If the search result data exceeds the configured amount
    /// the function reduces the number of vectors, reducing the amount of data sent.
    /// </summary>
    private (string augmentedContent, int newUserPromptTokens) BuildPromptAndData(string sessionId, string userPrompt, int conversationTokens, string augmentedContent)
    {
        
        string updatedAugmentedContent = "";

        //SharpToken only estimates token usage and often undercounts. Add a buffer of 300 tokens.
        int maxGPTTokens = 4096 - 300;

        //Create a new instance of SharpToken
        var encoding = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");

        //Get count of vectors on rag data
        List<int> ragVectors = encoding.Encode(augmentedContent);
        int ragTokens = ragVectors.Count;

        //Get count of vectors on user prompt (return)
        int promptTokens = encoding.Encode(userPrompt).Count;

        //Get count of vectors on conversation. This only counts the prompt and completion tokens. Does not include the tokens used to process the rag data.
        

        //If RAG data plus user prompt, plus conversation, plus tokens for completion is greater than max tokens, reduce by the size of data sent.
        if (ragTokens + promptTokens + conversationTokens + _maxCompletionTokens > maxGPTTokens)
        {

            //Calculate how many vectors we can pass
            int ragVectorsToTake = maxGPTTokens - promptTokens - _maxCompletionTokens - conversationTokens;

            //Get the reduced number vectors
            List<int> trimmedRagVectors = (List<int>)ragVectors.GetRange(0, ragVectorsToTake);

            //Trim the data so it will not go over GPT's max token limit.
            updatedAugmentedContent = encoding.Decode(trimmedRagVectors);

        }
        //If everthing + _maxCompletionTokens is less than 4097 then good to go.
        else if (ragTokens + promptTokens + conversationTokens + _maxCompletionTokens < maxGPTTokens)
        {
            int index = _sessions.FindIndex(s => s.SessionId == sessionId);

            //Return all of the content
            updatedAugmentedContent = augmentedContent;
        }


        return (augmentedContent: updatedAugmentedContent, newUserPromptTokens: promptTokens);

    }

    /// <summary>
    /// Get current conversation from newest to oldest up to max conversation tokens and add to the prompt
    /// </summary>
    private (string conversation, int tokens) GetChatSessionConversation(string sessionId, string userPrompt)
    {

        
        int? tokensUsed = 0;

        List<string> conversationBuilder = new List<string>();

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        List<Message> messages = _sessions[index].Messages;

        //Start at the end of the list and work backwards
        for (int i = messages.Count - 1; i >= 0; i--)
        {

            tokensUsed += messages[i].Tokens;

            if (tokensUsed > _maxConversationTokens)
                break;

            conversationBuilder.Add(messages[i].Text);

        }

        //Invert the chat messages to put back into chronological order and output as string.        
        string conversation = string.Join(Environment.NewLine, conversationBuilder.Reverse<string>());

        //Add a new line if needed, then append the current userPrompt
        if (conversation.Length > 0)
            conversation += Environment.NewLine;

        conversation += userPrompt;


        return (conversation, (int)tokensUsed);


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
    private async Task AddPromptMessageAsync(string sessionId, string promptText)
    {
        Message promptMessage = new(sessionId, nameof(Participants.User), default, default, promptText);

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        _sessions[index].AddMessage(promptMessage);

        await _mongoDbService.InsertMessageAsync(promptMessage);
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
        _sessions[index].TokensUsed += completionMessage.PromptTokens;
        _sessions[index].TokensUsed += completionMessage.Tokens;

        await _mongoDbService.UpsertSessionBatchAsync(session: _sessions[index], promptMessage: promptMessage, completionMessage: completionMessage);

    }
}