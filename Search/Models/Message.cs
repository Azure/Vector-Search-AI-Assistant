using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Search.Models;

public record Message
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; }

    public string Type { get; set; }

    public string SessionId { get; set; }

    public DateTime TimeStamp { get; set; }

    public string Sender { get; set; }

    public int Tokens { get; set; }

    public int PromptTokens { get; set; }

    public string Text { get; set; }

    public Message(string sessionId, string sender, int? tokens, int? promptTokens, string text)
    {
        Id = Guid.NewGuid().ToString();
        Type = nameof(Message);
        SessionId = sessionId;
        Sender = sender;
        Tokens = tokens ?? 0;
        PromptTokens = promptTokens ?? 0;
        TimeStamp = DateTime.UtcNow;
        Text = text;
    }
}