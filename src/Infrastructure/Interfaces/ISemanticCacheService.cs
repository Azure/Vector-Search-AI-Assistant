using BuildYourOwnCopilot.Common.Models.Chat;
using BuildYourOwnCopilot.Service.Models.Chat;

namespace BuildYourOwnCopilot.Service.Interfaces
{
    public interface ISemanticCacheService
    {
        Task Initialize();

        Task<SemanticCacheItem> GetCacheItem(string userPrompt, List<Message> messageHistory);
        Task SetCacheItem(SemanticCacheItem cacheItem);
    }
}
