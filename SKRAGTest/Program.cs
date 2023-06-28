using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;

var tokens = GPT3Tokenizer.Encode(@"Many words map to one token, but some don't: indivisible.

Unicode characters like emojis may be split into many tokens containing the underlying bytes: 🤚🏾

Sequences of characters commonly found next to each other may be grouped together: 1234567890");


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

var result = await ragService.Summarize("", "Do you have some nice socks?\nYes, of course we have, and we also have some nice hats if you are interested.");
await host.RunAsync();