using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using VectorSearchAiAssistant.Service.Services;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;

[assembly: FunctionsStartup(typeof(Vectorize.Startup))]

namespace Vectorize
{
    public class Startup: FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
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

            builder.Services.AddSingleton<IOpenAiService, OpenAiService>((provider) =>
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

            builder.Services.AddSingleton<ICognitiveSearchServiceManagement, CognitiveSearchService>((provider) =>
            {
                var cognitiveSearchOptions = provider.GetRequiredService<IOptions<CognitiveSearch>>();

                if(cognitiveSearchOptions is null)
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
                        logger: provider.GetRequiredService<ILogger<CognitiveSearch>>()
                    );
                }
            });
        }
    }
}
