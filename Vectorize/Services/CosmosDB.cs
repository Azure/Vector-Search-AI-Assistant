using Microsoft.Azure.Cosmos;
using System.Text.Json;
using DataCopilot.Vectorize.Models;

namespace DataCopilot.Vectorize.Services;

public class CosmosDB
{
    private readonly CosmosClient _cosmosClient;

    public CosmosDB(string configuration)
    {
        _cosmosClient = new CosmosClient(configuration);
    }


    public async IAsyncEnumerable<DocumentVector> GetAllEmbeddings()
    {
        var container = _cosmosClient.GetContainer("database", "embedding");
        
        using var feedIterator = container.GetItemQueryIterator<DocumentVector>("SELECT * FROM c");

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync();
            foreach (var item in response)
            {
                yield return item;
            }
        }
    }
}