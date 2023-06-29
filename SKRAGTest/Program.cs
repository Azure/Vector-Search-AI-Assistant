﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using VectorSearchAiAssistant.Service.Models.Search;
using VectorSearchAiAssistant.SemanticKernel.Connectors.TextEmbedding;

var product = new Product(
    id: "00001",
    categoryId: "C48B4EF4-D352-4CD2-BCB8-CE89B7DFA642",
    categoryName: "Clothing, Socks",
    sku: "SO-R999-M",
    name: "Cosmic Racing Socks, M",
    description: "The product called Cosmic Racing Socks, M",
    price: 6.00,
    tags: new List<Tag>
    {
        new Tag(id: "51CD93BF-098C-4C25-9829-4AD42046D038", name: "Tag-25"),
        new Tag(id: "5D24B427-1402-49DE-B79B-5A7013579FBC", name: "Tag-76"),
        new Tag(id: "D4EC9C09-75F3-4ADD-A6EB-ACDD12C648FA", name: "Tag-153")
    });
product.entityType__ = "Product";

var embItem = EmbeddingUtility.Transform<Product>(product);

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