using Azure.Search.Documents.Indexes;

namespace VectorSearchAiAssistant.Common.Models.BusinessDomain
{
    public class EmbeddedEntity
    {
        [SearchableField(IsKey = true, IsFilterable = true)]
        public string id { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        [EmbeddingField(Label = "Entity (object) type")]
        public string entityType__ { get; set; }    // Since this applies to all business entities,  use a name that is unlikely to cause collisions with other properties
    }
}
