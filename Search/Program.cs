using Search.Options;
using Search.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterConfiguration();
builder.Services.AddRazorPages();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddServerSideBlazor();
builder.Services.RegisterServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();

static class ProgramExtensions
{
    public static void RegisterConfiguration(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddUserSecrets<Program>(optional: true, reloadOnChange: true);

        builder.Services.AddOptions<OpenAi>()
            .Bind(builder.Configuration.GetSection(nameof(OpenAi)));

        builder.Services.AddOptions<MongoDb>()
            .Bind(builder.Configuration.GetSection(nameof(MongoDb)));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        
        services.AddSingleton<OpenAiService, OpenAiService>((provider) =>
        {
            var openAiOptions = provider.GetRequiredService<IOptions<OpenAi>>();
            if (openAiOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<OpenAi>)} was not resolved through dependency injection.");
            }
            else
            {
                return new OpenAiService(
                    endpoint: openAiOptions.Value?.Endpoint ?? String.Empty,
                    key: openAiOptions.Value?.Key ?? String.Empty,
                    embeddingsDeployment: openAiOptions.Value?.EmbeddingsDeployment ?? String.Empty,
                    completionsDeployment: openAiOptions.Value?.CompletionsDeployment ?? String.Empty,
                    maxConversationTokens: openAiOptions.Value?.MaxConversationTokens ?? String.Empty,
                    maxCompletionTokens: openAiOptions.Value?.MaxCompletionTokens ?? String.Empty,
                    logger: provider.GetRequiredService<ILogger<OpenAiService>>()
                );
            }
        });

        services.AddSingleton<MongoDbService, MongoDbService>((provider) =>
        {
            var mongoDbOptions = provider.GetRequiredService<IOptions<MongoDb>>();
            if (mongoDbOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<MongoDb>)} was not resolved through dependency injection.");
            }
            else
            {
                return new MongoDbService(
                    connection: mongoDbOptions.Value?.Connection ?? String.Empty,
                    databaseName: mongoDbOptions.Value?.DatabaseName ?? String.Empty,
                    collectionNames: mongoDbOptions.Value?.CollectionNames ?? String.Empty,
                    maxVectorSearchResults: mongoDbOptions.Value?.MaxVectorSearchResults ?? String.Empty,
                    openAiService: provider.GetRequiredService<OpenAiService>(),
                    logger: provider.GetRequiredService<ILogger<MongoDbService>>()
                );
            }
        });
        services.AddSingleton<ChatService, ChatService>((provider) =>
        {
            var chatOptions = provider.GetRequiredService<IOptions<Chat>>();
            if (chatOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<Chat>)} was not resolved through dependency injection");
            }
            else
            {
                return new ChatService(
                    mongoDbService: provider.GetRequiredService<MongoDbService>(),
                    openAiService: provider.GetRequiredService<OpenAiService>(),
                    logger: provider.GetRequiredService<ILogger<ChatService>>()
                );
            }
        });
    }
}
