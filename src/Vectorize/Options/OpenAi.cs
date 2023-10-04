using Microsoft.Extensions.Logging;

namespace Vectorize.Options
{
    public record OpenAi
    {
        public string? Endpoint { get; set; }

        public string? Key { get; set; }

        public string? EmbeddingsDeployment { get; set; }

        public string? MaxTokens { get; set; }

        public ILogger? Logger { get; set; }
    }
}
