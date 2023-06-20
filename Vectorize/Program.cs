using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vectorize.Options;
using Vectorize.Services;



    var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults(builder =>
        {

            builder.Services.AddLogging();


            builder.Services.AddOptions<OpenAi>()
                    .Configure<IConfiguration>((settings, configuration) =>
                    {
                        configuration.GetSection(nameof(OpenAi)).Bind(settings);
                    });



            builder.Services.AddOptions<MongoDb>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(MongoDb)).Bind(settings);
                });

        })
        .ConfigureAppConfiguration(con =>
        {
            con.AddUserSecrets<Program>(optional: true, reloadOnChange: true);
        })
        .ConfigureServices(s =>
        {

            s.AddSingleton<OpenAiService, OpenAiService>((provider) =>
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

            s.AddSingleton<MongoDbService, MongoDbService>((provider) =>
            {
                var mongoOptions = provider.GetRequiredService<IOptions<MongoDb>>();

                if (mongoOptions is null)
                {
                    throw new ArgumentException($"{nameof(IOptions<MongoDb>)} was not resolved through dependency injection.");
                }
                else
                {
                    return new MongoDbService
                    (
                        connection: mongoOptions.Value?.Connection ?? string.Empty,
                        databaseName: mongoOptions.Value?.DatabaseName ?? string.Empty,
                        collectionNames: mongoOptions.Value?.CollectionNames ?? string.Empty,
                        openAiService: provider.GetRequiredService<OpenAiService>(),
                        logger: provider.GetRequiredService<ILogger<MongoDb>>()
                    );
                }
            });

        })
        .Build();

    host.Run();
