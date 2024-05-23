using VectorSearchAiAssistant.Common.Models.Chat;
using VectorSearchAiAssistant.Service.Models.Chat;

namespace VectorSearchAiAssistant.Service.Interfaces
{
    public interface ISemanticCacheService
    {
        Task Initialize();

        Task<SemanticCacheItem> GetCacheItem(string userPrompt, List<Message> messageHistory);
        Task SetCacheItem(SemanticCacheItem cacheItem);
    }
}
