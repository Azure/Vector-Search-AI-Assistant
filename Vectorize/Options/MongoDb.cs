using Microsoft.Extensions.Logging;
using Vectorize.Services;

namespace Vectorize.Options
{
    public record MongoDb
    {
        public string? Connection { get; set; }
        public string? DatabaseName { get; set; } 

        public string? CollectionNames { get; set; }

        public OpenAiService? OpenAiService { get; set; }

        public ILogger? Logger { get; set; }

    }
}
