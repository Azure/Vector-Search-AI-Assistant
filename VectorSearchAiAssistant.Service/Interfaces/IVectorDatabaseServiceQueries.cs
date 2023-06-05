using Microsoft.Extensions.Logging;

namespace VectorSearchAiAssistant.Service.Interfaces;

public interface IVectorDatabaseServiceQueries
{
    Task<string> VectorSearchAsync(float[] embeddings);
}