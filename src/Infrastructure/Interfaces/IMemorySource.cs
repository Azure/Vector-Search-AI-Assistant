namespace BuildYourOwnCopilot.Service.Interfaces
{
    public interface IMemorySource
    {
        Task<List<string>> GetMemories();
    }
}
