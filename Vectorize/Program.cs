using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Service.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.Services.AddLogging();
        

        builder.Services.AddOptions<OpenAi>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(nameof(OpenAi)).Bind(settings);
            });

        builder.Services.AddOptions<CognitiveSearch>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(nameof(CognitiveSearch)).Bind(settings);
            });

        builder.Services.AddOptions<CosmosDb>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(nameof(CosmosDb)).Bind(settings);
            });
    })
    .ConfigureAppConfiguration(con =>
    {
        con.AddUserSecrets<Program>(optional: true, reloadOnChange: false);
    })
    .ConfigureServices(s =>
    {
        s.AddSingleton<IOpenAiService, OpenAiService>((provider) =>
        {
            var openAiOptions = provider.GetRequiredService<IOptions<OpenAi>>();

            if (openAiOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<OpenAi>)} was not resolved through dependency injection.");
            }
            else
            {
                return new OpenAiService
                (
                    endpoint: openAiOptions.Value?.Endpoint ?? string.Empty,
                    key: openAiOptions.Value?.Key ?? string.Empty,
                    completionsDeployment: openAiOptions.Value?.CompletionsDeployment ?? string.Empty,
                    embeddingsDeployment: openAiOptions.Value?.EmbeddingsDeployment ?? string.Empty,
                    maxConversationBytes: openAiOptions.Value?.MaxConversationBytes ?? string.Empty,
                    logger: provider.GetRequiredService<ILogger<OpenAi>>()
                );
            }

        });

        s.AddSingleton<IVectorDatabaseServiceManagement, VectorDatabaseService>((provider) =>
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

        s.AddSingleton<ICosmosDbService, CosmosDbService>((provider) =>
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
    })
    .Build();

host.Run();
