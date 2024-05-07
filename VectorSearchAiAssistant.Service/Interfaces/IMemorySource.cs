namespace VectorSearchAiAssistant.Service.Interfaces
{
    public interface IMemorySource
    {
        Task<List<string>> GetMemories();
    }
}
