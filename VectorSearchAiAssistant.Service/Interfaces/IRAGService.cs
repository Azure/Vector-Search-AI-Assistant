using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VectorSearchAiAssistant.Service.Models.Search;

namespace VectorSearchAiAssistant.Service.Interfaces
{
    public interface IRAGService
    {
        bool IsInitialized { get; }

        Task<(string Completion, int UserPromptTokens, int ResponseTokens, float[]? UserPromptEmbedding)> GetResponse(string userPrompt, string interactionHistory);

        Task<string> Summarize(string sessionId, string userPrompt);

        int MaxConversationBytes { get; }

        Task AddMemory<T>(T item, string itemName, Action<T, float[]> vectorizer);

        Task RemoveMemory<T>(T item);
    }
}
