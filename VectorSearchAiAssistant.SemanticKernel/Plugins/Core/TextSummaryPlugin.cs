using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Orchestration;
using System.ComponentModel;

namespace VectorSearchAiAssistant.SemanticKernel.Plugins.Core
{
    public class TextSummaryPlugin
    {
        private readonly ISKFunction _summarizeConversation;

        public TextSummaryPlugin(
            string promptTemplate,
            int maxTokens,
            IKernel kernel)
        {
            _summarizeConversation = kernel.CreateSemanticFunction(
                promptTemplate,
                pluginName: nameof(TextSummaryPlugin),
                description: "Given a text, summarize the text.",
                requestSettings: new OpenAIRequestSettings
                {
                    MaxTokens = maxTokens,
                    Temperature = 0.1,
                    TopP = 0.5
                });
        }

        [SKFunction]
        public async Task<string> SummarizeConversationAsync(
            SKContext context)
        {
            var result = await _summarizeConversation.InvokeAsync(context);
            return result.GetValue<string>() ?? string.Empty;
        }
    }
}
