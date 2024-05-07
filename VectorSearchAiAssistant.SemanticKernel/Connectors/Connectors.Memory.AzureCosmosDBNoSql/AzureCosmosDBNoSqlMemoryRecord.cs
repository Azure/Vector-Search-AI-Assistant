using Microsoft.SemanticKernel.Memory;
using System.Text.Json.Serialization;

namespace VectorSearchAiAssistant.SemanticKernel.Connectors.AzureCosmosDBNoSql;

#pragma warning disable SKEXP0001

public class AzureCosmosDBNoSqlMemoryRecord
{
    /// <summary>
    /// Unique identifier of the memory record.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Metadata associated with the memory record.
    /// </summary>
    [JsonPropertyName("metadata")]
    public AzureCosmosDBNoSqlMemoryRecordMetadata Metadata { get; set; }

    /// <summary>
    /// Embedding associated with the memory record.
    /// </summary>
    [JsonPropertyName("embedding")]
    public required float[] Embedding { get; set; }

    /// <summary>
    /// Optional timestamp associated with the memory record.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCosmosDBNoSqlMemoryRecord"/> class.
    /// </summary>
    /// <param name="memoryRecord"><see cref="MemoryRecord"/> instance to copy values from.</param>
    public AzureCosmosDBNoSqlMemoryRecord(MemoryRecord memoryRecord)
    {
        this.Id = memoryRecord.Key;
        this.Metadata = new AzureCosmosDBNoSqlMemoryRecordMetadata(memoryRecord.Metadata);
        this.Embedding = memoryRecord.Embedding.ToArray();
        this.Timestamp = memoryRecord.Timestamp?.UtcDateTime;
    }

    /// <summary>
    /// Returnes a new instance of <see cref="MemoryRecord"/> with the same values as this instance.
    /// </summary>
    /// <returns><see cref="MemoryRecord"/> instance copied from the current instance.</returns>
    public MemoryRecord ToMemoryRecord() =>
        new(
            this.Metadata.ToMemoryRecordMetadata(),
            this.Embedding,
            this.Id,
            this.Timestamp
        );
}