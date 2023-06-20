namespace Search.Options;

public record OpenAi
{
    public required string Endpoint { get; init; }

    public required string Key { get; init; }

    public required string EmbeddingsDeployment { get; init; }

    public required string CompletionsDeployment { get; init; }

    public required string MaxConversationTokens { get; init; }

    public required string MaxCompletionTokens { get; init; }

    public required ILogger Logger { get; init; }
}