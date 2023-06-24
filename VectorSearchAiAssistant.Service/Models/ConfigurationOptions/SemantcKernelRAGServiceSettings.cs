namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record SemanticKernelRAGServiceSettings
    {
        public record OpenAISettings
        {
            public required string CompletionsDeployment { get; set; }
            public required string EmbeddingsDeployment { get; init; }
            public required int MaxConversationBytes { get; init; }
            public required string Endpoint { get; init; }
            public required string Key { get; init; }
        }

        public record CognitiveSearchSettings
        {
            public required string IndexName { get; set; }
            public required int MaxVectorSearchResults { get; init; }
            public required string Endpoint { get; init; }
            public required string Key { get; init; }
        }

        public required OpenAISettings OpenAI { get; set; }
        public required CognitiveSearchSettings CognitiveSearch { get; set; }
    }
}