using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorSearchAiAssistant.Service.Interfaces
{
    public interface IRAGService
    {
        Task<(string Completion, int UserPromptTokens, int ResponseTokens, float[] UserPromptEmbedding)> GetResponse(string userPrompt);

        int MaxConversationBytes { get; }
    }
}
