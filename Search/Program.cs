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

        builder.Services.AddOptions<ChatManagerSettings>()
            .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:ChatManager"));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<ICosmosDbService, CosmosDbService>();
        services.AddSingleton<IRAGService, SemanticKernelRAGService>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IChatManager, ChatManager>();
    }
}
