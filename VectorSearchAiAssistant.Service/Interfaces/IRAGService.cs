using VectorSearchAiAssistant.Common.Interfaces;
using VectorSearchAiAssistant.Common.Models.Chat;

namespace VectorSearchAiAssistant.Service.Interfaces
{
    public interface IRAGService
    {
        bool IsInitialized { get; }

        Task<(string Completion, string UserPrompt, int UserPromptTokens, int ResponseTokens, float[]? UserPromptEmbedding)> GetResponse(string userPrompt, List<Message> messageHistory);

        Task<string> Summarize(string sessionId, string userPrompt);

        Task AddMemory(IItemTransformer itemTransformer);

        Task RemoveMemory(object item);
    }
}
