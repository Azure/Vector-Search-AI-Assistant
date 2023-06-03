using Microsoft.Extensions.Logging;

namespace VectorSearchAiAssistant.Service.Interfaces;

public interface ICognitiveSearchServiceQueries
{
    Task<string> VectorSearchAsync(float[] embeddings, ILogger logger);
}