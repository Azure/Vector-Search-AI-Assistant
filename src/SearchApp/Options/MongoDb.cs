using SearchApp.Services;

namespace SearchApp.Options
{
    public record MongoDb
    {
        public string? Connection { get; set; }

        public string? DatabaseName { get; set; }

        public string? CollectionNames { get; set; }

        public string? MaxVectorSearchResults { get; set; }

        public OpenAiService? OpenAiService { get; set; }

        public ILogger? Logger { get; set; }

    }
}