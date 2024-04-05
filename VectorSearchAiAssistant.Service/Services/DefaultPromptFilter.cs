using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0001

namespace VectorSearchAiAssistant.Service.Services
{
    public class DefaultPromptFilter : IPromptFilter
    {
        public string RenderedPrompt => _renderedPrompt;

        private string _renderedPrompt = string.Empty;

        public void OnPromptRendered(PromptRenderedContext context)
        {
            _renderedPrompt = context.RenderedPrompt;
        }

        public void OnPromptRendering(PromptRenderingContext context)
        {
        }
    }
}
