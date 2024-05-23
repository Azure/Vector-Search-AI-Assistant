namespace VectorSearchAiAssistant.Common.Interfaces
{
    public interface IItemTransformerFactory
    {
        IItemTransformer CreateItemTransformer(dynamic item);
    }
}
