using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using VectorSearchAiAssistant.Service.Interfaces;
using VectorSearchAiAssistant.Service.Models.ConfigurationOptions;

namespace VectorSearchAiAssistant.Service.Services
{
    public class DurableSystemPromptService : ISystemPromptService
    {
        readonly DurableSystemPromptServiceSettings _settings;
        readonly BlobContainerClient _storageClient;
        Dictionary<string, string> _prompts = new Dictionary<string, string>();

        public DurableSystemPromptService(
            IOptions<DurableSystemPromptServiceSettings> settings)
        {
            _settings = settings.Value;

            var blobServiceClient = new BlobServiceClient(_settings.BlobStorageConnection);
            _storageClient = blobServiceClient.GetBlobContainerClient(_settings.BlobStorageContainer);
        }

        public async Task<string> GetPrompt(string promptName, bool forceRefresh = false)
        {
            if (_prompts.ContainsKey(promptName) && !forceRefresh)
                return _prompts[promptName];

            return null;
        }

        private string GetFilePath(string promptName)
        {
            var tokens = promptName.Split('.');

            //var folderPath = $"/{string.Join('/', tokens.Take(tokens.Length - 1)}/{tokens[tokens.Length - 1]}.txt";
            return null;
        }
    }
}
