namespace BuildYourOwnCopilot.Service.Models.ConfigurationOptions
{
    public record SemanticCacheServiceSettings
    {
        public int ConversationContextMaxTokens { get; set; }
    }
}
