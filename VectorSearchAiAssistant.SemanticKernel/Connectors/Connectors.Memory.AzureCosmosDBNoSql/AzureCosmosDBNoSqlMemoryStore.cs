using Microsoft.Azure.Cosmos;
using Microsoft.SemanticKernel.Memory;
using VectorSearchAiAssistant.Common.Interfaces;

namespace VectorSearchAiAssistant.SemanticKernel.Connectors.AzureCosmosDBNoSql;

#pragma warning disable SKEXP0001

public class AzureCosmosDBNoSqlMemoryStore : IMemoryStore
{
    private readonly ICosmosDBVectorStoreService _cosmosDbVectorStoreService;
    public AzureCosmosDBNoSqlMemoryStore(
        ICosmosDBVectorStoreService cosmosDbVectorStoreService)
    {
        _cosmosDbVectorStoreService = cosmosDbVectorStoreService;
    }

    public async Task CreateCollectionAsync(string collectionName, CancellationToken cancellationToken = default) =>
        await _cosmosDbVectorStoreService.CreateCollection(collectionName);

    public async Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default) =>
        await _cosmosDbVectorStoreService.DeleteCollection(collectionName);

    public async Task<bool> DoesCollectionExistAsync(string collectionName, CancellationToken cancellationToken = default) =>
        await Task.FromResult(_cosmosDbVectorStoreService.CollectionExists(collectionName));

    public Task<MemoryRecord?> GetAsync(string collectionName, string key, bool withEmbedding = false, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public IAsyncEnumerable<MemoryRecord> GetBatchAsync(string collectionName, IEnumerable<string> keys, bool withEmbeddings = false, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public IAsyncEnumerable<string> GetCollectionsAsync(CancellationToken cancellationToken = default) =>
        _cosmosDbVectorStoreService.GetCollections().ToAsyncEnumerable();

    public Task<(MemoryRecord, double)?> GetNearestMatchAsync(string collectionName, ReadOnlyMemory<float> embedding, double minRelevanceScore = 0, bool withEmbedding = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesAsync(string collectionName, ReadOnlyMemory<float> embedding, int limit, double minRelevanceScore = 0, bool withEmbeddings = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(string collectionName, string key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveBatchAsync(string collectionName, IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> UpsertAsync(string collectionName, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        //var memoryRecord = new AzureCosmosDBNoSqlMemoryRecord(record);

        throw new NotImplementedException();
    }

    public IAsyncEnumerable<string> UpsertBatchAsync(string collectionName, IEnumerable<MemoryRecord> records, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

