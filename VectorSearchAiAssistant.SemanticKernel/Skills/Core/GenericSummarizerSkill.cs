using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Text;
using System.ComponentModel;

namespace VectorSearchAiAssistant.SemanticKernel.Skills.Core
{
    public class GenericSummarizerSkill
    {
        private readonly ISKFunction _summarizeConversation;
        private readonly int _maxTokens = 10;

        public GenericSummarizerSkill(
            string promptTemplate,
            int maxTokens,
            IKernel kernel)
        {
            _summarizeConversation = kernel.CreateSemanticFunction(
                promptTemplate,
                skillName: nameof(GenericSummarizerSkill),
                description: "Given a section of a conversation transcript, summarize the part of the conversation",
                maxTokens: maxTokens,
                temperature: 0.1,
                topP: 0.5);
        }

        [SKFunction("Given a section of a conversation transcript, summarize the part of the conversation")]
        public Task<SKContext> SummarizeConversationAsync(
            [Description("A short or long conversation transcript.")] string input,
            SKContext context)
        {
            return _summarizeConversation.InvokeAsync(input);
        }
    }
}
