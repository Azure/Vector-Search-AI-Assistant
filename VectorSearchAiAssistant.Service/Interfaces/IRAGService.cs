using VectorSearchAiAssistant.Service.Models.Chat;
using VectorSearchAiAssistant.Service.Models.Search;

namespace VectorSearchAiAssistant.Service.Interfaces
{
    public interface IRAGService
    {
        bool IsInitialized { get; }

        Task<(string Completion, int UserPromptTokens, int ResponseTokens, float[]? UserPromptEmbedding)> GetResponse(string userPrompt, List<Message> messageHistory);

        Task<string> Summarize(string sessionId, string userPrompt);

        Task AddMemory<T>(T item, string itemName, Action<T, float[]> vectorizer) where T : EmbeddedEntity;

        Task RemoveMemory<T>(T item);
    }
}
