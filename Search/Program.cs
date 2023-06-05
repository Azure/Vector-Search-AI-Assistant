using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
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
        builder.Services.AddOptions<CosmosDb>()
            .Bind(builder.Configuration.GetSection(nameof(CosmosDb)));

        builder.Services.AddOptions<OpenAi>()
            .Bind(builder.Configuration.GetSection(nameof(OpenAi)));

        builder.Services.AddOptions<CognitiveSearch>()
            .Bind(builder.Configuration.GetSection(nameof(CognitiveSearch)));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<ICosmosDbService, CosmosDbService>((provider) =>
        {
            var cosmosDbOptions = provider.GetRequiredService<IOptions<CosmosDb>>();
            if (cosmosDbOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<CosmosDb>)} was not resolved through dependency injection.");
            }
            else
            {
                return new CosmosDbService(
                    endpoint: cosmosDbOptions.Value?.Endpoint ?? String.Empty,
                    key: cosmosDbOptions.Value?.Key ?? String.Empty,
                    databaseName: cosmosDbOptions.Value?.Database ?? String.Empty,
                    containerNames: cosmosDbOptions.Value?.Containers ?? String.Empty,
                    logger: provider.GetRequiredService<ILogger<CosmosDb>>()
                );
            }
        });
        services.AddSingleton<IOpenAiService, OpenAiService>((provider) =>
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
                    maxConversationBytes: openAiOptions.Value?.MaxConversationBytes ?? String.Empty,
                    logger: provider.GetRequiredService<ILogger<OpenAiService>>()
                );
            }
        });
        services.AddSingleton<IVectorDatabaseServiceQueries, VectorDatabaseService>((provider) =>
        {
            var cognitiveSearchOptions = provider.GetRequiredService<IOptions<CognitiveSearch>>();

            if (cognitiveSearchOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<CognitiveSearch>)} was not resolved through dependency injection.");
            }
            else
            {
                return new VectorDatabaseService
                (
                    azureSearchAdminKey: cognitiveSearchOptions.Value?.AdminKey ?? string.Empty,
                    azureSearchServiceEndpoint: cognitiveSearchOptions.Value?.Endpoint ?? string.Empty,
                    azureSearchIndexName: cognitiveSearchOptions.Value?.IndexName ?? string.Empty,
                    maxVectorSearchResults: cognitiveSearchOptions.Value?.MaxVectorSearchResults ?? string.Empty,
                    logger: provider.GetRequiredService<ILogger<CognitiveSearch>>()
                );
            }
        });
        services.AddSingleton<IChatService, ChatService>();
    }
}
