namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record CosmosDbSettingsBase
    {
        public required string Endpoint { get; init; }

        public required string Key { get; init; }

        public required string Database { get; init; }

        public bool EnableTracing { get; init; }
    }
}
