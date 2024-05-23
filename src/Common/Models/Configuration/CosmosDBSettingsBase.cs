namespace VectorSearchAiAssistant.Common.Models.Configuration
{
    public record CosmosDBSettingsBase
    {
        public required string Endpoint { get; init; }

        public required string Key { get; init; }

        public required string Database { get; init; }

        public bool EnableTracing { get; init; }
    }
}
