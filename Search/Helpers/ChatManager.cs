using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Chat;

namespace Search.Helpers
{
    public class ChatManager : IChatManager
    {
        /// <summary>
        /// All data is cached in the _sessions List object.
        /// </summary>
        private static List<Session> _sessions = new();
        private readonly IChatService _chatService;

        public ChatManager(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Returns list of chat session ids and names for left-hand nav to bind to (display Name and ChatSessionId as hidden)
        /// </summary>
        public async Task<List<Session>> GetAllChatSessionsAsync()
        {
            return _sessions = await _chatService.GetAllChatSessionsAsync();
        }

        /// <summary>
        /// Returns the chat messages to display on the main web page when the user selects a chat from the left-hand nav
        /// </summary>
        public async Task<List<Message>> GetChatSessionMessagesAsync(string sessionId)
        {
            List<Message> chatMessages;

            if (_sessions.Count == 0)
            {
                return Enumerable.Empty<Message>().ToList();
            }

            var index = _sessions.FindIndex(s => s.SessionId == sessionId);

            chatMessages = await _chatService.GetChatSessionMessagesAsync(sessionId);

            // Cache results
            _sessions[index].Messages = chatMessages;

            return chatMessages;
        }

        /// <summary>
        /// User creates a new Chat Session.
        /// </summary>
        public async Task CreateNewChatSessionAsync()
        {
            var session = await _chatService.CreateNewChatSessionAsync();
            _sessions.Add(session);
        }

        /// <summary>
        /// Rename the Chat Ssssion from "New Chat" to the summary provided by OpenAI
        /// </summary>
        public async Task RenameChatSessionAsync(string sessionId, string newChatSessionName, bool onlyUpdateLocalSessionsCollection = false)
        {
            ArgumentNullException.ThrowIfNull(sessionId);

            var index = _sessions.FindIndex(s => s.SessionId == sessionId);
            _sessions[index].Name = newChatSessionName;

            if (!onlyUpdateLocalSessionsCollection)
            {
                await _chatService.RenameChatSessionAsync(sessionId, newChatSessionName);
            }
        }

        /// <summary>
        /// User deletes a chat session
        /// </summary>
        public async Task DeleteChatSessionAsync(string sessionId)
        {
            ArgumentNullException.ThrowIfNull(sessionId);

            var index = _sessions.FindIndex(s => s.SessionId == sessionId);
            _sessions.RemoveAt(index);

            await _chatService.DeleteChatSessionAsync(sessionId);
        }

        /// <summary>
        /// Receive a prompt from a user, Vectorize it from _openAIService Get a completion from _openAiService
        /// </summary>
        public async Task<string> GetChatCompletionAsync(string sessionId, string userPrompt)
        {
            ArgumentNullException.ThrowIfNull(sessionId);

            var completion =  await _chatService.GetChatCompletionAsync(sessionId, userPrompt);
            // Refresh the local messages cache:
            await GetChatSessionMessagesAsync(sessionId);
            return completion;
        }

        public async Task<string> SummarizeChatSessionNameAsync(string sessionId, string prompt)
        {
            ArgumentNullException.ThrowIfNull(sessionId);

            var response = await _chatService.SummarizeChatSessionNameAsync(sessionId, prompt);

            await RenameChatSessionAsync(sessionId, response, true);

            return response;
        }

        /// <summary>
        /// Rate an assistant message. This can be used to discover useful AI responses for training, discoverability, and other benefits down the road.
        /// </summary>
        public async Task<Message> RateMessageAsync(string id, string sessionId, bool? rating)
        {
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(sessionId);

            return await _chatService.RateMessageAsync(id, sessionId, rating);
        }
    }
}
