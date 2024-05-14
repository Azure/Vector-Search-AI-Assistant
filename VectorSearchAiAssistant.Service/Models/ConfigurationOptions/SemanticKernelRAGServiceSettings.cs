﻿using VectorSearchAiAssistant.Common.Models.Configuration;
using VectorSearchAiAssistant.Common.Models.ConfigurationOptions;
using VectorSearchAiAssistant.SemanticKernel.Models;

namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record SemanticKernelRAGServiceSettings
    {
        public required OpenAISettings OpenAI { get; init; }
        public required AISearchSettings AISearch { get; init; }
        public required CosmosDBVectorStoreServiceSettings CosmosDBVectorStore { get; init; }
        public required VectorSearchSettings KnowledgeRetrieval {  get; init; }
        public required VectorSearchSettings SemanticCacheRetrieval { get; init; }
        public required TokenTextSplitterServiceSettings TextSplitter {  get; init; }
        public required SemanticCacheServiceSettings SemanticCache { get; init; }
    }
}