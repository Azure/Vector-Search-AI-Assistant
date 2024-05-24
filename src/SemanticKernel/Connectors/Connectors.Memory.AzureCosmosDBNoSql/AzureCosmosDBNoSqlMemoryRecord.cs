using Microsoft.SemanticKernel.Memory;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BuildYourOwnCopilot.SemanticKernel.Connectors.AzureCosmosDBNoSql;

#pragma warning disable SKEXP0001

public class AzureCosmosDBNoSqlMemoryRecord
{
    /// <summary>
    /// Unique identifier of the memory record.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Logical partition identifier.
    /// </summary>
    public required string PartitionKey { get; set; }

    /// <summary>
    /// Metadata associated with the memory record.
    /// </summary>
    public AzureCosmosDBNoSqlMemoryRecordMetadata Metadata { get; set; }

    /// <summary>
    /// Embedding associated with the memory record.
    /// </summary>
    public required float[] Embedding { get; set; }

    /// <summary>
    /// Optional timestamp associated with the memory record.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

#pragma warning disable CS8618

    public AzureCosmosDBNoSqlMemoryRecord()
    {
    }

#pragma warning restore CS8618

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCosmosDBNoSqlMemoryRecord"/> class.
    /// </summary>
    /// <param name="memoryRecord"><see cref="MemoryRecord"/> instance to copy values from.</param>
    [SetsRequiredMembers]
    public AzureCosmosDBNoSqlMemoryRecord(MemoryRecord memoryRecord)
    {
        Id = memoryRecord.Metadata.Id;
        PartitionKey = memoryRecord.Key;
        Metadata = new AzureCosmosDBNoSqlMemoryRecordMetadata(memoryRecord.Metadata);
        Embedding = memoryRecord.Embedding.ToArray();
        Timestamp = memoryRecord.Timestamp ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Returnes a new instance of <see cref="MemoryRecord"/> with the same values as this instance.
    /// </summary>
    /// <returns><see cref="MemoryRecord"/> instance copied from the current instance.</returns>
    public MemoryRecord ToMemoryRecord() =>
        new(
            Metadata.ToMemoryRecordMetadata(),
            Embedding,
            PartitionKey,
            Timestamp
        );
}