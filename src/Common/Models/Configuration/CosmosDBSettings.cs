namespace BuildYourOwnCopilot.Common.Models.Configuration
{
    public record CosmosDBSettings : CosmosDBSettingsBase
    {
        public required string Containers { get; init; }

        public required string MonitoredContainers { get; init; }

        public required string ChangeFeedLeaseContainer { get; init; }

        public required string ChangeFeedSourceContainer { get; init; }
    }
}
