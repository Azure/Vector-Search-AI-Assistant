using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using DataCopilot.Search.Components;
using DataCopilot.Search.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace DataCopilot.Search.Services;

/// <summary>
/// Service to access Azure OpenAI.
/// </summary>
public class OpenAiService
{
    private readonly string _embeddingsModelOrDeployment = string.Empty;
    private readonly string _completionsModelOrDeployment = string.Empty;
    private readonly int _maxConversationBytes = default;
    private readonly ILogger _logger;
    private readonly OpenAIClient _client;    



    //System prompts to send with user prompts to instruct the model for chat session
    private readonly string _systemPrompt = @"
        You are an AI assistant that helps people find information.
        Provide concise answers that are polite and professional." + Environment.NewLine;

    private readonly string _systemPromptRetailAssistant = @"
        You are an Assistant is an intelligent chatbot designed to help users answer their questions related to the contents 
        of their documents containing information on products, customers and sales orders in JSON format.
        Instructions:
        - Only answer questions related to the documents provided below,
        - Don't reference any other product/customer/sales order data,
        - If you're unsure of an answer, you can say ""I don't know"" or ""I'm not sure"" and recommend users search themselves.

        Text of relevant documents:";

    //System prompt to send with user prompts to instruct the model for summarization
    private readonly string _summarizePrompt = @"
        Summarize this prompt in one or two words to use as a label in a button on a web page. Output words only." + Environment.NewLine;


    /// <summary>
    /// Gets the maximum number of tokens to limit chat conversation length.
    /// </summary>
    public int MaxConversationBytes
    {
        get => _maxConversationBytes;
    }

    /// <summary>
    /// Creates a new instance of the service.
    /// </summary>
    /// <param name="endpoint">Endpoint URI.</param>
    /// <param name="key">Account key.</param>
    /// <param name="embeddingsDeployment">Name of the model deployment for generating embeddings.</param>
    /// <param name="completionsDeployment">Name of the model deployment for generating completions.</param>
    /// <param name="maxConversationBytes">Maximum number of bytes to limit conversation history sent for a completion.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when endpoint, key, deploymentName, or maxConversationBytes is either null or empty.</exception>
    /// <remarks>
    /// This constructor will validate credentials and create a HTTP client instance.
    /// </remarks>
    public OpenAiService(string endpoint, string key, string embeddingsDeployment, string completionsDeployment, string maxConversationBytes, ILogger logger)
    {
        //ArgumentException.ThrowIfNullOrEmpty(endpoint);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(embeddingsDeployment);
        ArgumentException.ThrowIfNullOrEmpty(completionsDeployment);
        ArgumentException.ThrowIfNullOrEmpty(maxConversationBytes);

        _embeddingsModelOrDeployment = embeddingsDeployment;
        _completionsModelOrDeployment = completionsDeployment;
        _maxConversationBytes = int.TryParse(maxConversationBytes, out _maxConversationBytes) ? _maxConversationBytes : 2000;

        _logger = logger;

        OpenAIClientOptions options = new OpenAIClientOptions()
        {
            Retry =
            {
                Delay = TimeSpan.FromSeconds(2),
                MaxRetries = 10,
                Mode = RetryMode.Exponential
            }
        };

        //Use this as endpoint in configuration to use non-Azure Open AI endpoint and OpenAI model names
        if (endpoint.Contains("api.openai.com"))
            _client = new OpenAIClient(key, options);
        else
            _client = new(new Uri(endpoint), new AzureKeyCredential(key), options);
        

    }

    /// <summary>
    /// Sends a prompt to the deployed OpenAI embeddings model and returns an array of vectors as a response.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt message to generated embeddings on.</param>
    /// <returns>Response from the OpenAI model as an array of vectors along with tokens for the prompt and response.</returns>
    public async Task<(float[] response, int responseTokens)> GetEmbeddingsAsync(string sessionId, string input)
    {

        //await HttpRequestAsync(input);

        float[] embedding = new float[0];
        int responseTokens = 0;

        try
        {
            EmbeddingsOptions options = new EmbeddingsOptions(input)
            {
                Input = input,
                User = sessionId
            };


            var response = await _client.GetEmbeddingsAsync(_embeddingsModelOrDeployment, options);


            Embeddings embeddings = response.Value;

            responseTokens = embeddings.Usage.TotalTokens;

            embedding = embeddings.Data[0].Embedding.ToArray();

            return (
                response: embedding,
                responseTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return (
                response: embedding,
                responseTokens);
        }
    }

    /// <summary>
    /// Sends a prompt to the deployed OpenAI LLM model and returns the response.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt message to send to the deployment.</param>
    /// <returns>Response from the OpenAI model along with tokens for the prompt and response.</returns>
    public async Task<(string response, int promptTokens, int responseTokens)> GetChatCompletionAsync(string sessionId, string userPrompt, string documents)
    {

        try
        {
        
            ChatMessage systemMessage = new ChatMessage(ChatRole.System, _systemPromptRetailAssistant + documents);
            ChatMessage userMessage = new ChatMessage(ChatRole.User, userPrompt);


            ChatCompletionsOptions options = new()
            {

                Messages =
                {
                    systemMessage,
                    userMessage
                },
                User = sessionId,
                Temperature = 0.5f, //0.3f,
                NucleusSamplingFactor = 0.95f, //0.5f,
                FrequencyPenalty = 0,
                PresencePenalty = 0
            };

            Response<ChatCompletions> completionsResponse = await _client.GetChatCompletionsAsync(_completionsModelOrDeployment, options);
        

            ChatCompletions completions = completionsResponse.Value;

            return (
                response: completions.Choices[0].Message.Content,
                promptTokens: completions.Usage.PromptTokens,
                responseTokens: completions.Usage.CompletionTokens
            );

        }
        catch ( Exception ex ) 
        { 
        
            _logger.LogError(ex.Message);
        
        }

        return ("", 0, 0);
    }

    /// <summary>
    /// Sends the existing conversation to the OpenAI model and returns a two word summary.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="userPrompt">The first User Prompt and Completion to send to the deployment.</param>
    /// <returns>Summarization response from the OpenAI model deployment.</returns>
    public async Task<string> SummarizeAsync(string sessionId, string userPrompt)
    {

        ChatMessage systemMessage = new ChatMessage(ChatRole.System, _summarizePrompt);
        ChatMessage userMessage = new ChatMessage(ChatRole.User, userPrompt);

        ChatCompletionsOptions options = new()
        {
            Messages = {
                systemMessage,
                userMessage
            },
            User = sessionId,
            MaxTokens = 200,
            Temperature = 0.0f,
            NucleusSamplingFactor = 1.0f,
            FrequencyPenalty = 0,
            PresencePenalty = 0
        };

        Response<ChatCompletions> completionsResponse = await _client.GetChatCompletionsAsync(_completionsModelOrDeployment, options);

        ChatCompletions completions = completionsResponse.Value;
        string output = completions.Choices[0].Message.Content;

        //Remove all non-alpha numeric characters (Turbo has a habit of putting things in quotes even when you tell it not to
        string summary = Regex.Replace(output, @"[^a-zA-Z0-9\s]", "");

        return summary;
    }
}