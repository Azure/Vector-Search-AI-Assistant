using Microsoft.Extensions.Logging;

namespace DataCopilot.Vectorize.Options
{
    public class Redis
    {
        public string? ConnectionString { get; set; }
        public ILogger? Logger { get; set; }
    }
}
