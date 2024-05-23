namespace VectorSearchAiAssistant.Common.Interfaces
{
    public interface ICosmosDBVectorStoreService
    {
        Task CreateCollection(string collectionName, CancellationToken cancellationToken = default);

        Task DeleteCollection(string collectionName, CancellationToken cancellationToken = default);

        bool CollectionExists(string collectionName);

        List<string> GetCollections();

        Task<T> UpsertItem<T>(string collectionName, T item) where T : class;

        IAsyncEnumerable<T> GetNearestRecords<T>(string collectionName, float[] embedding, double similarityScore, int topN) where T : class;
    }
}
