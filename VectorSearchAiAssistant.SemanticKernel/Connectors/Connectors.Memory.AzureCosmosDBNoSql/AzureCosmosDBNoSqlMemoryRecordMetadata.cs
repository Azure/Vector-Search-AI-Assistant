using Microsoft.SemanticKernel.Memory;
using System.Text.Json.Serialization;

namespace VectorSearchAiAssistant.SemanticKernel.Connectors.AzureCosmosDBNoSql;

#pragma warning disable SKEXP0001

/// <summary>
/// An Azure Cosmos DB NoSQL memory record.
/// </summary>
public class AzureCosmosDBNoSqlMemoryRecordMetadata
{
    /// <summary>
    /// Indicates whether the source data used to calculate embeddings is stored in the local
    /// storage provider or is available through and external service, such as web site, MS Graph, etc.
    /// </summary>
    public bool IsReference { get; set; }

    /// <summary>
    /// A value used to understand which external service owns the data, to avoid storing the information
    /// inside the URI. E.g. this could be "MSTeams", "WebSite", "GitHub", etc.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ExternalSourceName { get; set; }

    /// <summary>
    /// Unique identifier. The format of the value is domain specific, so it can be a URL, a GUID, etc.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Optional title describing the content. Note: the title is not indexed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Description { get; set; }

    /// <summary>
    /// Source text, available only when the memory is not an external source.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Text { get; set; }

    /// <summary>
    /// Optional custom metadata.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string AdditionalMetadata { get; set; }

#pragma warning disable CS8618

    public AzureCosmosDBNoSqlMemoryRecordMetadata()
    {
    }

#pragma warning restore CS8618

    /// <summary>
    /// Initializes an instance of <see cref="AzureCosmosDBNoSqlMemoryRecordMetadata"/> using an instance of <see cref="MemoryRecordMetadata"/>.
    /// </summary>
    /// <param name="memoryRecordMetadata"><see cref="MemoryRecordMetadata"/> instance to copy values from.</param>
    public AzureCosmosDBNoSqlMemoryRecordMetadata(MemoryRecordMetadata memoryRecordMetadata)
    {
        IsReference = memoryRecordMetadata.IsReference;
        ExternalSourceName = memoryRecordMetadata.ExternalSourceName;
        Id = memoryRecordMetadata.Id;
        Description = memoryRecordMetadata.Description;
        Text = memoryRecordMetadata.Text;
        AdditionalMetadata = memoryRecordMetadata.AdditionalMetadata;
    }

    /// <summary>
    /// Returnes a new instance of <see cref="MemoryRecordMetadata"/> with the same values as this instance.
    /// </summary>
    /// <returns><see cref="MemoryRecordMetadata"/> instance copied from the current instance.</returns>
    public MemoryRecordMetadata ToMemoryRecordMetadata() =>
        new (
            IsReference,
            ExternalSourceName,
            Id,
            Description,
            Text,
            AdditionalMetadata);
}
