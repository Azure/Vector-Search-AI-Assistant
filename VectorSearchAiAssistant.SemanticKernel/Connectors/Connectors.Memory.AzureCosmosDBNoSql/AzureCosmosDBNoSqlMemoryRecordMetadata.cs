using Microsoft.SemanticKernel.Memory;
using System.Text.Json.Serialization;

namespace VectorSearchAiAssistant.SemanticKernel.Connectors.AzureCosmosDBNoSql;

#pragma warning disable SKEXP0001

/// <summary>
/// An Azure Cosmos DB NoSQL memory record.
/// </summary>
/// <param name="memoryRecordMetadata"><see cref="MemoryRecordMetadata"/> instance to copy values from.</param>
public class AzureCosmosDBNoSqlMemoryRecordMetadata(MemoryRecordMetadata memoryRecordMetadata)
{
    /// <summary>
    /// Indicates whether the source data used to calculate embeddings is stored in the local
    /// storage provider or is available through and external service, such as web site, MS Graph, etc.
    /// </summary>
    [JsonPropertyName("is_reference")]
    public bool IsReference { get; set; } = memoryRecordMetadata.IsReference;

    /// <summary>
    /// A value used to understand which external service owns the data, to avoid storing the information
    /// inside the URI. E.g. this could be "MSTeams", "WebSite", "GitHub", etc.
    /// </summary>
    [JsonPropertyName("external_source_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ExternalSourceName { get; set; } = memoryRecordMetadata.ExternalSourceName;

    /// <summary>
    /// Unique identifier. The format of the value is domain specific, so it can be a URL, a GUID, etc.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = memoryRecordMetadata.Id;

    /// <summary>
    /// Optional title describing the content. Note: the title is not indexed.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Description { get; set; } = memoryRecordMetadata.Description;

    /// <summary>
    /// Source text, available only when the memory is not an external source.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Text { get; set; } = memoryRecordMetadata.Text;

    /// <summary>
    /// Optional custom metadata.
    /// </summary>
    [JsonPropertyName("additional_metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string AdditionalMetadata { get; set; } = memoryRecordMetadata.AdditionalMetadata;

    /// <summary>
    /// Returnes a new instance of <see cref="MemoryRecordMetadata"/> with the same values as this instance.
    /// </summary>
    /// <returns><see cref="MemoryRecordMetadata"/> instance copied from the current instance.</returns>
    public MemoryRecordMetadata ToMemoryRecordMetadata() =>
        new (
            this.IsReference,
            this.ExternalSourceName,
            this.Id,
            this.Description,
            this.Text,
            this.AdditionalMetadata);
}
