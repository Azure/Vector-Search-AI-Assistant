using VectorSearchAiAssistant.SemanticKernel.Models;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.MemorySource;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Service.Services;
using VectorSearchAiAssistant.Service.Services.Text;

namespace ChatServiceWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplicationInsightsTelemetry();

            builder.Services.AddOptions<CosmosDbSettings>()
                .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:CosmosDB"));

            builder.Services.AddOptions<AISearchSettings>()
                .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:AISearch"));

            builder.Services.AddOptions<SemanticKernelRAGServiceSettings>()
                .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI"));

            builder.Services.AddSingleton<IAISearchService, AISearchService>();
            builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
            builder.Services.AddSingleton<IRAGService, SemanticKernelRAGService>();
            builder.Services.AddSingleton<IChatService, ChatService>();

            // Simple, static system prompt service
            //builder.Services.AddSingleton<ISystemPromptService, InMemorySystemPromptService>();

            // System prompt service backed by an Azure blob storage account
            builder.Services.AddOptions<DurableSystemPromptServiceSettings>()
                .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:DurableSystemPrompt"));
            builder.Services.AddSingleton<ISystemPromptService, DurableSystemPromptService>();

            builder.Services.AddOptions<AzureAISearchMemorySourceSettings>()
                .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:AISearchMemorySource"));
            builder.Services.AddTransient<IMemorySource, AzureAISearchMemorySource>();

            builder.Services.AddOptions<BlobStorageMemorySourceSettings>()
                .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:BlobStorageMemorySource"));
            builder.Services.AddTransient<IMemorySource, BlobStorageMemorySource>();

            builder.Services.AddSingleton<ITokenizerService, MicrosoftBPETokenizerService>();
            builder.Services.ActivateSingleton<ITokenizerService>();

            builder.Services.AddOptions<TokenTextSplitterServiceSettings>()
                .Bind(builder.Configuration.GetSection("MSCosmosDBOpenAI:TextSplitter"));
            builder.Services.AddSingleton<ITextSplitterService, TokenTextSplitterService>();

            builder.Services.AddScoped<ChatEndpoints>();

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseExceptionHandler(exceptionHandlerApp
                    => exceptionHandlerApp.Run(async context
                        => await Results.Problem().ExecuteAsync(context)));

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