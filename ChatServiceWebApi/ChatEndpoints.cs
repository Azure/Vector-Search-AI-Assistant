using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.Chat;

namespace ChatServiceWebApi
{
    public class ChatEndpoints
    {
        private readonly IChatService _chatService;

        public ChatEndpoints(IChatService chatService)
        {
            _chatService = chatService;
        }

        public void Map(WebApplication app)
        {
            app.MapGet("/sessions/", async () => await _chatService.GetAllChatSessionsAsync());

            app.MapGet("/sessions/{sessionId}/messages",
                    async (string sessionId) => await _chatService.GetChatSessionMessagesAsync(sessionId));

            app.MapPost("/sessions/{sessionId}/message/{messageId}/rate", 
                async (string messageId, string sessionId, bool? rating, HttpContext context) =>
                await _chatService.RateMessageAsync(messageId, sessionId, rating));
        }
    }
}
