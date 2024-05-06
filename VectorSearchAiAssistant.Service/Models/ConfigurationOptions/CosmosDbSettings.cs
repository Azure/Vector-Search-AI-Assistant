namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record CosmosDbSettings : CosmosDbSettingsBase
    {
        public required string Containers { get; init; }

        public required string MonitoredContainers { get; init; }

        public required string ChangeFeedLeaseContainer { get; init; }

        public required string ChangeFeedSourceContainer { get; init; }
    }
}
