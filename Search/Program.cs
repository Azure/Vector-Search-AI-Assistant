using Search.Options;
using Search.Services;
using Microsoft.Extensions.Options;

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

        builder.Services.AddOptions<Redis>()
            .Bind(builder.Configuration.GetSection(nameof(Redis)));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<CosmosDbService, CosmosDbService>((provider) =>
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
                    maxConversationBytes: openAiOptions.Value?.MaxConversationBytes ?? String.Empty,
                    logger: provider.GetRequiredService<ILogger<OpenAiService>>()
                );
            }
        });
        services.AddSingleton<RedisService, RedisService>((provider) =>
        {
            var redisOptions = provider.GetRequiredService<IOptions<Redis>>();
            if (redisOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<Redis>)} was not resolved through dependency injection.");
            }
            else
            {
                return new RedisService(
                    connection: redisOptions.Value?.Connection ?? String.Empty,
                    logger: provider.GetRequiredService<ILogger<RedisService>>()
                );
            }
        });
        services.AddSingleton<ChatService>();
    }
}
