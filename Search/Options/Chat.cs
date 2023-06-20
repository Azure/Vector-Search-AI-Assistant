namespace Search.Services;


public record Chat
{
    public required MongoDbService MongoDbService { get; set; }

    public required OpenAiService OpenAiService { get; set; }

    public required ILogger Logger { get; init; }
}
