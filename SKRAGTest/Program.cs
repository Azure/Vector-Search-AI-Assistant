using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Service.Services;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddOptions<CosmosDbSettings>()
    .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:CosmosDB"));

builder.Services.AddOptions<SemanticKernelRAGServiceSettings>()
    .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI"));

builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddSingleton<IRAGService, SemanticKernelRAGService>();

builder.Services.AddOptions<DurableSystemPromptServiceSettings>()
    .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:DurableSystemPrompt"));
builder.Services.AddSingleton<ISystemPromptService, DurableSystemPromptService>();

var host = builder.Build();

var ragService = host.Services.GetService<IRAGService>();

var result = await ragService.GetResponse("Do you have socks?");

await host.RunAsync();