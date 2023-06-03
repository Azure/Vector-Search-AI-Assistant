using Microsoft.Extensions.Logging;

namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record OpenAi
    {
        public required string Endpoint { get; init; }

        public required string Key { get; init; }

        public required string EmbeddingsDeployment { get; init; }

        public required string CompletionsDeployment { get; init; }

        public required string MaxConversationBytes { get; init; }

        public required ILogger? Logger { get; init; }
    }
}
