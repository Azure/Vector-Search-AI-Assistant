using BuildYourOwnCopilot.Common.Interfaces;
using BuildYourOwnCopilot.Common.Models.Chat;

namespace BuildYourOwnCopilot.Service.Interfaces
{
    public interface IRAGService
    {
        bool IsInitialized { get; }

        Task<CompletionResult> GetResponse(string userPrompt, List<Message> messageHistory);

        Task<string> Summarize(string sessionId, string userPrompt);

        Task AddMemory(IItemTransformer itemTransformer);

        Task RemoveMemory(IItemTransformer itemTransformer);
    }
}
