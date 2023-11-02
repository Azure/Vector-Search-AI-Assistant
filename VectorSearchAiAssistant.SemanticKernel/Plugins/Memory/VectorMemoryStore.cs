using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Memory;

namespace VectorSearchAiAssistant.SemanticKernel.Plugins.Memory
{
    public class VectorMemoryStore
    {
        public VectorMemoryStore(
            IMemoryStore memoryStore,
            ILogger<VectorMemoryStore> logger) 
        { 
        }
    }
}
