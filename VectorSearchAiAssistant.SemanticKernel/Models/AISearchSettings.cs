namespace VectorSearchAiAssistant.SemanticKernel.Models
{
    public record AISearchSettings
    {
        public required string IndexName { get; init; }
        public required int MaxVectorSearchResults { get; init; }
        public required double MinRelevance { get; init; }
        public required string Endpoint { get; init; }
        public required string Key { get; init; }
    }
}
