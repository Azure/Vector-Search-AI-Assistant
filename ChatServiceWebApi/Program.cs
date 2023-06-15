
using Microsoft.Extensions.Options;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Service.Services;

namespace ChatServiceWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddOptions<CosmosDb>()
                .Bind(builder.Configuration.GetSection(nameof(CosmosDb)));

            builder.Services.AddOptions<OpenAi>()
                .Bind(builder.Configuration.GetSection(nameof(OpenAi)));

            builder.Services.AddOptions<CognitiveSearch>()
                .Bind(builder.Configuration.GetSection(nameof(CognitiveSearch)));

            builder.Services.AddOptions<SemanticKernelRAGServiceSettings>()
                .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI"));

            builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>((provider) =>
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
            builder.Services.AddSingleton<IOpenAiService, OpenAiService>((provider) =>
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
            builder.Services.AddSingleton<IVectorDatabaseServiceQueries, CognitiveSearchService>((provider) =>
            {
                var cognitiveSearchOptions = provider.GetRequiredService<IOptions<CognitiveSearch>>();

                if (cognitiveSearchOptions is null)
                {
                    throw new ArgumentException($"{nameof(IOptions<CognitiveSearch>)} was not resolved through dependency injection.");
                }
                else
                {
                    return new CognitiveSearchService
                    (
                        azureSearchAdminKey: cognitiveSearchOptions.Value?.AdminKey ?? string.Empty,
                        azureSearchServiceEndpoint: cognitiveSearchOptions.Value?.Endpoint ?? string.Empty,
                        azureSearchIndexName: cognitiveSearchOptions.Value?.IndexName ?? string.Empty,
                        maxVectorSearchResults: cognitiveSearchOptions.Value?.MaxVectorSearchResults ?? string.Empty,
                        logger: provider.GetRequiredService<ILogger<CognitiveSearch>>(),
                        // Explicitly setting createIndexIfNotExists value to false because the Blazor app freezes and
                        // does not render the UI when the service attempts to check if the index exists.
                        createIndexIfNotExists: false
                    );
                }
            });
            builder.Services.AddSingleton<IRAGService, SemanticKernelRAGService>();
            builder.Services.AddSingleton<IChatService, ChatService>();

            builder.Services.AddScoped<ChatEndpoints>();

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseAuthorization();

            // Map the chat REST endpoints:
            using (var scope = app.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetService<ChatEndpoints>();
                service?.Map(app);
            }

            app.Run();
        }
    }
}