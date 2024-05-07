namespace VectorSearchAiAssistant.Common.Models.Configuration
{
    public record CosmosDBVectorStoreServiceSettings : CosmosDBSettingsBase
    {
        /// <summary>
        /// The name of the Cosmos DB container that stores the vector embeddings used in the main RAG flow.
        /// </summary>
        public required string MainIndexName { get; init; }

        /// <summary>
        /// The name of the Cosmos DB container that stores the vector embeddings used in the semantic cache.
        /// </summary>
        public required string SemanticCacheIndexName { get; init; }
    }
}
