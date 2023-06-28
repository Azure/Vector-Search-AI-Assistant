using VectorSearchAiAssistant.Service.Models.Chat;

namespace VectorSearchAiAssistant.Service.Interfaces
{
    public interface IRAGService
    {
        bool IsInitialized { get; }

        Task<(string Completion, int UserPromptTokens, int ResponseTokens, float[]? UserPromptEmbedding)> GetResponse(string userPrompt, List<Message> messageHistory);

        Task<string> Summarize(string sessionId, string userPrompt);

        int MaxConversationBytes { get; }

        Task AddMemory<T>(T item, string itemName, Action<T, float[]> vectorizer);

        Task RemoveMemory<T>(T item);
    }
}
