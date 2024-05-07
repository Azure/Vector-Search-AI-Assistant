using VectorSearchAiAssistant.Common.Models.Configuration;
using VectorSearchAiAssistant.SemanticKernel.Models;

namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record SemanticKernelRAGServiceSettings
    {
        public required OpenAISettings OpenAI { get; init; }
        public required AISearchSettings AISearch { get; init; }
        public required CosmosDBVectorStoreServiceSettings CosmosDBVectorStore { get; init; }
    }
}