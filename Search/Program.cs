using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Search.Helpers;
using VectorSearchAiAssistant.Service.Services;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;

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
        builder.Services.AddOptions<CosmosDbSettings>()
            .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:CosmosDB"));

        builder.Services.AddOptions<SemanticKernelRAGServiceSettings>()
                .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI"));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<ICosmosDbService, CosmosDbService>((provider) =>
        {
            var cosmosDbOptions = provider.GetRequiredService<IOptions<CosmosDbSettings>>();
            if (cosmosDbOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<CosmosDbSettings>)} was not resolved through dependency injection.");
            }
            else
            {
                return new CosmosDbService(
                    endpoint: cosmosDbOptions.Value?.Endpoint ?? String.Empty,
                    key: cosmosDbOptions.Value?.Key ?? String.Empty,
                    databaseName: cosmosDbOptions.Value?.Database ?? String.Empty,
                    containerNames: cosmosDbOptions.Value?.Containers ?? String.Empty,
                    logger: provider.GetRequiredService<ILogger<CosmosDbService>>()
                );
            }
        });
        services.AddSingleton<IOpenAiService, OpenAiService>((provider) =>
        {
            var openAiOptions = provider.GetRequiredService<IOptions<SemanticKernelRAGServiceSettings>>();
            if (openAiOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<SemanticKernelRAGServiceSettings>)} was not resolved through dependency injection.");
            }
            else
            {
                return new OpenAiService(
                    endpoint: openAiOptions.Value?.OpenAI.Endpoint ?? String.Empty,
                    key: openAiOptions.Value?.OpenAI.Key ?? String.Empty,
                    embeddingsDeployment: openAiOptions.Value?.OpenAI.EmbeddingsDeployment ?? String.Empty,
                    completionsDeployment: openAiOptions.Value?.OpenAI.CompletionsDeployment ?? String.Empty,
                    maxConversationBytes: openAiOptions.Value?.OpenAI.MaxConversationBytes ?? String.Empty,
                    logger: provider.GetRequiredService<ILogger<OpenAiService>>()
                );
            }
        });
        services.AddSingleton<IVectorDatabaseServiceQueries, CognitiveSearchService>((provider) =>
        {
            var cognitiveSearchOptions = provider.GetRequiredService<IOptions<SemanticKernelRAGServiceSettings>>();

            if (cognitiveSearchOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<SemanticKernelRAGServiceSettings>)} was not resolved through dependency injection.");
            }
            else
            {
                return new CognitiveSearchService
                (
                    azureSearchAdminKey: cognitiveSearchOptions.Value?.CognitiveSearch.Key ?? string.Empty,
                    azureSearchServiceEndpoint: cognitiveSearchOptions.Value?.CognitiveSearch.Endpoint ?? string.Empty,
                    azureSearchIndexName: cognitiveSearchOptions.Value?.CognitiveSearch.IndexName ?? string.Empty,
                    maxVectorSearchResults: cognitiveSearchOptions.Value?.CognitiveSearch.MaxVectorSearchResults ?? string.Empty,
                    logger: provider.GetRequiredService<ILogger<CognitiveSearchService>>(),
                    // Explicitly setting createIndexIfNotExists value to false because the Blazor app freezes and
                    // does not render the UI when the service attempts to check if the index exists.
                    createIndexIfNotExists:false
                );
            }
        });
        services.AddSingleton<IRAGService, SemanticKernelRAGService>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IChatManager, ChatManager>();
    }
}
