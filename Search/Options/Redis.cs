namespace DataCopilot.Search.Options
{
    public record Redis
    {
        public required string ConnectionString { get; init; }

        public required ILogger Logger { get; init; }
    }
}
