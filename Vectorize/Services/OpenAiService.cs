using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Vectorize.Services;

public class OpenAiService
{
    private string _openAIEndpoint = string.Empty;
    private string _openAIKey = string.Empty;
    private string _openAIEmbeddings = string.Empty;
    private int _openAIMaxTokens = default;

    private OpenAIClient _client;


    public OpenAiService()
    {
        _openAIEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint") + "";
        _openAIKey = Environment.GetEnvironmentVariable("OpenAIKey") + "";
        _openAIEmbeddings = Environment.GetEnvironmentVariable("EmbeddingsDeployment") + "";
        string maxTokens = Environment.GetEnvironmentVariable("OpenAIMaxTokens") + "";
        _openAIMaxTokens = int.TryParse(maxTokens, out _openAIMaxTokens) ? _openAIMaxTokens : 8191;

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
        if (_openAIEndpoint.Contains("api.openai.com"))
            _client = new OpenAIClient(_openAIKey, options);
        else
            _client = new(new Uri(_openAIEndpoint), new AzureKeyCredential(_openAIKey), options);

    }

    public async Task<float[]?> GetEmbeddingsAsync(dynamic data, ILogger log)
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
            log.LogError(ex.Message);
            return null;
        }
    }
}
