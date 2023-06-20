using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Vectorize.Services;

public class OpenAiService
{
    private readonly string _openAIEndpoint = string.Empty;
    private readonly string _openAIKey = string.Empty;
    private readonly string _openAIEmbeddings = string.Empty;
    private readonly int _openAIMaxTokens = default;
    private readonly ILogger _logger;

    private readonly OpenAIClient? _client;


    public OpenAiService(string endpoint, string key, string embeddingsDeployment, string maxTokens, ILogger logger)
    {

        
        _openAIEndpoint = endpoint;
        _openAIKey = key;
        _openAIEmbeddings = embeddingsDeployment;
        _openAIMaxTokens = int.TryParse(maxTokens, out _openAIMaxTokens) ? _openAIMaxTokens : 8191;
        
        _logger = logger;


        OpenAIClientOptions clientOptions = new OpenAIClientOptions()
        {
            Retry =
            {
                Delay = TimeSpan.FromSeconds(2),
                MaxRetries = 10,
                Mode = RetryMode.Exponential
            }
        };

        try
        {

            //Use this as endpoint in configuration to use non-Azure Open AI endpoint and OpenAI model names
            if (_openAIEndpoint.Contains("api.openai.com"))
                _client = new OpenAIClient(_openAIKey, clientOptions);
            else
                _client = new(new Uri(_openAIEndpoint), new AzureKeyCredential(_openAIKey), clientOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"OpenAIService Constructor failure: {ex.Message}");
        }
    }

    public async Task<float[]?> GetEmbeddingsAsync(dynamic data)
    {
        try
        {
            EmbeddingsOptions options = new EmbeddingsOptions(data)
            {
                Input = data
            };

            var response = await _client.GetEmbeddingsAsync(_openAIEmbeddings, options);

            Embeddings embeddings = response.Value;

            float[] embedding = embeddings.Data[0].Embedding.ToArray();

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetEmbeddingsAsync Exception: {ex.Message}");
            return null;
        }
    }
}
