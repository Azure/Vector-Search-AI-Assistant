namespace DataCopilot.Search.Options
{
    public record Redis
    {
        public required string Connection { get; init; }

        public required ILogger Logger { get; init; }
    }
}
