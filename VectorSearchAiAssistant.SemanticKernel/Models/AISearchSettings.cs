namespace VectorSearchAiAssistant.SemanticKernel.Models
{
    public record AISearchSettings
    {
        public required string Endpoint { get; init; }
        public required string Key { get; init; }
    }
}
