using Microsoft.Extensions.Logging;

namespace VectorSearchAiAssistant.Service.Interfaces;

public interface ICognitiveSearchServiceManagement
{
    Task InsertVector(object document, ILogger logger);
    Task InsertVectors(IEnumerable<object> documents, ILogger logger);
    Task DeleteVector(object document, ILogger logger);
}