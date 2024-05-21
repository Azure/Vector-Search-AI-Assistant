using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Memory;
using System.ComponentModel;
using VectorSearchAiAssistant.Common.Models;
using VectorSearchAiAssistant.Common.Models.Chat;
using VectorSearchAiAssistant.Common.Models.ConfigurationOptions;
using VectorSearchAiAssistant.Common.Text;
using VectorSearchAiAssistant.SemanticKernel.Chat;
using VectorSearchAiAssistant.SemanticKernel.Models;
using VectorSearchAiAssistant.SemanticKernel.Plugins.Memory;

#pragma warning disable SKEXP0001

namespace VectorSearchAiAssistant.SemanticKernel.Plugins.Core
{
    /// <summary>
    /// AdvancedChatPlugin provides the capability to build the context for chat completions by recalling object information from the long term memory using vector-based similarity.
    /// Optionally, a short-term, volatile memory can be also used to enhance the result set.
    /// </summary>
    public sealed class KnowledgeManagementContextPlugin
    {
        private readonly VectorMemoryStore _longTermMemory;
        private readonly VectorMemoryStore _shortTermMemory;
        private readonly string _systemPrompt;
        private readonly VectorSearchSettings _searchSettings;
        private readonly OpenAISettings _openAISettings;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of the AdvancedChatPlugin
        /// </summary>
        public KnowledgeManagementContextPlugin(
            VectorMemoryStore longTermMemory,
            VectorMemoryStore shortTermMemory,
            string systemPrompt,
            VectorSearchSettings searchSettings,
            OpenAISettings openAISettings,
            ILogger logger)
        {
            _longTermMemory = longTermMemory;
            _shortTermMemory = shortTermMemory;
            _systemPrompt = systemPrompt;
            _searchSettings = searchSettings;
            _openAISettings = openAISettings;
            _logger = logger;
        }

        /// <summary>
        /// Builds the context used for chat completions.
        /// </summary>
        /// <example>
        /// <param name="userPrompt">The input text to find related memories for.</param>
        [KernelFunction(name: "BuildContext")]
        public async Task<string> BuildContextAsync(
            [Description("The user prompt for which the context is being built.")] string userPrompt,
            [Description("The history of messages in the current conversation.")] List<Message> messageHistory)
        {
            _logger.LogTrace("Searching memories in with minimum relevance '{1}'", _searchSettings.MinRelevance);

            var userPromptEmbedding = await _longTermMemory.GetEmbedding(userPrompt);

            // Search memory
            List<MemoryQueryResult> memories = await _longTermMemory
                .GetNearestMatches(userPrompt, _searchSettings.MaxVectorSearchResults, _searchSettings.MinRelevance)
                .ToListAsync()
                .ConfigureAwait(false);

            var combinedMemories = memories.ToList();
            if (_shortTermMemory != null)
            {
                var shortTermMemories = await _shortTermMemory
                    .GetNearestMatches(userPromptEmbedding, _searchSettings.MaxVectorSearchResults, _searchSettings.MinRelevance)
                    .ToListAsync()
                    .ConfigureAwait(false);

                combinedMemories = combinedMemories
                    .Concat(shortTermMemories)
                    .OrderByDescending(r => r.Relevance)
                    .ToList();
            }

            if (combinedMemories.Count == 0)
            {
                _logger.LogWarning("Neither the long-term store nor the short-term store contain any matching memories.");
            }

            _logger.LogTrace("Done looking for memories");

            var memoryTypes = ModelRegistry.Models.ToDictionary(m => m.Key, m => m.Value.Type);
            var context = new ContextBuilder(
                    _openAISettings.CompletionsDeploymentMaxTokens,
                    memoryTypes!,
                    promptOptimizationSettings: _openAISettings.PromptOptimization)
                        .WithSystemPrompt(
                            _systemPrompt)
                        .WithMemories(
                            combinedMemories.Select(x => x.Metadata.AdditionalMetadata).ToList())
                        .WithMessageHistory(
                            messageHistory.Select(m => (new AuthorRole(m.Sender.ToLower()), m.Text.NormalizeLineEndings())).ToList())
                        .Build();

            return context;
        }
    }
}
