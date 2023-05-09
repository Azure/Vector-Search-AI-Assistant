using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DataCopilot.Vectorize.Services;

public class OpenAI
{
    private string openAIEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint");
    private string openAIKey = Environment.GetEnvironmentVariable("OpenAIKey");
    private string openAIEmbeddings = Environment.GetEnvironmentVariable("EmbeddingsDeployment");
    private int openAIMaxTokens = int.Parse(Environment.GetEnvironmentVariable("OpenAIMaxTokens"));

    private OpenAIClient client;

    

    public OpenAI()
    {
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
        if (openAIEndpoint.Contains("api.openai.com"))
            client = new OpenAIClient(openAIKey, options);
        else
            client = new(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey), options);

    }

    public async Task<float[]> GetEmbeddingsAsync(dynamic data, ILogger log)
    {
        try
        {
            EmbeddingsOptions options = new EmbeddingsOptions(data)
            {
                Input = data
            };

            var response = await client.GetEmbeddingsAsync(openAIEmbeddings, options);

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
