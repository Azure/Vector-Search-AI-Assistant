using VectorSearchAiAssistant.Common.Interfaces;

namespace VectorSearchAiAssistant.Common.Services
{
    public class ItemTransformerFactory : IItemTransformerFactory
    {
        public IItemTransformer CreateItemTransformer(dynamic item) =>
            new ModelRegistryItemTransformer(item);
    }
}
