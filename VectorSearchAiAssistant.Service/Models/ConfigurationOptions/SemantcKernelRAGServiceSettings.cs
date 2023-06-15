namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record SemanticKernelRAGServiceSettings
    {
        public string OpenAIKey { get; init; }
        public string OpenAIEmbeddingDeploymentName { get; init; }
        public string OpenAICompletionDeploymentName { get; init; }
        public string OpenAIEndpoint { get; init; }
        public string CognitiveSearchKey { get; init; }
        public string CognitiveSearchEndpoint { get; init; }
    }
}