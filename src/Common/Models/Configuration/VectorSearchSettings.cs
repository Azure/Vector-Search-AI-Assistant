namespace VectorSearchAiAssistant.Common.Models.ConfigurationOptions
{
    public record VectorSearchSettings
    {
        public required string IndexName { get; init; }
        public required int MaxVectorSearchResults { get; init; }
        public required double MinRelevance { get; init; }
    }
}
