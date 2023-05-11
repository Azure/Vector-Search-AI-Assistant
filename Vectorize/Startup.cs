using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using DataCopilot.Vectorize.Services;
using DataCopilot.Vectorize.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(DataCopilot.Vectorize.Startup))]

namespace DataCopilot.Vectorize
{
    public class Startup: FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {

            builder.Services.AddOptions<OpenAi>()
                 .Configure<IConfiguration>((settings, configuration) =>
                 {
                     configuration.GetSection(nameof(OpenAi)).Bind(settings);
                 });



            builder.Services.AddOptions<Redis>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(Redis)).Bind(settings);
                });

            builder.Services.AddSingleton<OpenAiService, OpenAiService>((provider) =>
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
                        embeddingsDeployment: openAiOptions.Value?.EmbeddingsDeployment ?? string.Empty,
                        maxTokens: openAiOptions.Value?.MaxTokens ?? string.Empty,
                        logger: provider.GetRequiredService<ILogger<OpenAi>>()
                    );
                }

            });

            builder.Services.AddSingleton<RedisService, RedisService>((provider) =>
            {
                var redisOptions = provider.GetRequiredService<IOptions<Redis>>();

                if(redisOptions is null)
                {
                    throw new ArgumentException($"{nameof(IOptions<Redis>)} was not resolved through dependency injection.");
                }
                else
                {
                    return new RedisService
                    (
                        redisConnectionString: redisOptions.Value?.ConnectionString ?? string.Empty,
                        logger: provider.GetRequiredService<ILogger<Redis>>()
                    );
                }
            });

        }
    }
}
