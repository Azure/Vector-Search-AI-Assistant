using VectorSearchAiAssistant.SemanticKernel.Models;

namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record SemanticKernelRAGServiceSettings
    {
        public required OpenAISettings OpenAI { get; init; }
        public required AISearchSettings AISearch { get; init; }
    }
}