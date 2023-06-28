using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;
using System.Runtime;
using System.Text.Json;

namespace VectorSearchAiAssistant.SemanticKernel.Skills.Core
{
    /// <summary>
    /// TextEmbeddingObjectMemorySkill provides a skill to recall object information from the long term memory using vector-based similarity.
    /// </summary>
    /// <example>
    /// Usage: kernel.ImportSkill("memory", new TextEmbeddingObjectMemorySkill());
    /// Examples:
    /// SKContext["input"] = "what is the capital of France?"
    /// {{memory.recall $input }} => "Paris"
    /// </example>
    public sealed class TextEmbeddingObjectMemorySkill
    {
        /// <summary>
        /// The vector embedding of the last text input submitted to the Recall method.
        /// Can only be read once, to avoid inconsistencies across multiple calls to Recall.
        /// </summary>
        public IEnumerable<float>? LastInputTextEmbedding { get
            {
                var result = _lastInputTextEmbedding;
                _lastInputTextEmbedding = null;
                return result;
            } }

        private const string DefaultCollection = "generic";
        private const double DefaultRelevance = 0.7;
        private const int DefaultLimit = 1;

        private IEnumerable<float>? _lastInputTextEmbedding;

        /// <summary>
        /// Creates a new instance of the TextEmbeddingMemorySkill
        /// </summary>
        public TextEmbeddingObjectMemorySkill()
        {
        }

        /// <summary>
        /// Vector search and return up to N memories related to the input text
        /// </summary>
        /// <example>
        /// SKContext["input"] = "what is the capital of France?"
        /// {{memory.recall $input }} => "Paris"
        /// </example>
        /// <param name="text">The input text to find related memories for.</param>
        /// <param name="collection">Memories collection to search.</param>
        /// <param name="relevance">The relevance score, from 0.0 to 1.0, where 1.0 means perfect match.</param>
        /// <param name="limit">The maximum number of relevant memories to recall.</param>
        /// <param name="context">Contains the memory to search.</param>
        [SKFunction("Vector search and return up to N memories related to the input text")]
        public async Task<string> RecallAsync(
            [Description("The input text to find related memories for")] string text,
            [Description("Memories collection to search"), DefaultValue(DefaultCollection)] string collection,
            [Description("The relevance score, from 0.0 to 1.0, where 1.0 means perfect match"), DefaultValue(DefaultRelevance)] double? relevance,
            [Description("The maximum number of relevant memories to recall"), DefaultValue(DefaultLimit)] int? limit,
            SKContext context)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(collection, nameof(collection));
            relevance ??= DefaultRelevance;
            limit ??= DefaultLimit;

            context.Log.LogTrace("Searching memories in collection '{0}', relevance '{1}'", collection, relevance);

            // Search memory
            List<MemoryQueryResult> memories = await context.Memory
                .SearchAsync(collection, text, limit: limit.Value, minRelevanceScore: relevance.Value, withEmbeddings: true, cancellationToken: context.CancellationToken)
                .ToListAsync(context.CancellationToken)
                .ConfigureAwait(false);

            //By convention, the first item in the result is the embedding of the input text.
            //Once SK develops a more standardized way to expose embeddings, this should be removed.
            _lastInputTextEmbedding = memories.First().Embedding?.Vector;

            if (memories.Count == 1)
            {
                context.Log.LogWarning("Memories not found in collection: {0}", collection);
                return string.Empty;
            }

            context.Log.LogTrace("Done looking for memories in collection '{0}')", collection);
            return JsonSerializer.Serialize(memories.Skip(1).Select(x => x.Metadata.AdditionalMetadata));
        }
    }
}
