namespace VectorSearchAiAssistant.Common.Interfaces
{
    public interface ICosmosDBVectorStoreService
    {
        Task CreateCollection(string collectionName, CancellationToken cancellationToken = default);

        Task DeleteCollection(string collectionName, CancellationToken cancellationToken = default);

        bool CollectionExists(string collectionName);

        List<string> GetCollections();
    }
}
