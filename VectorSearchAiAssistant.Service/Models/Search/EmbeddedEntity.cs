using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorSearchAiAssistant.Service.Models.Search
{
    public class EmbeddedEntity
    {
        [FieldBuilderIgnore]
        public float[]? vector { get; set; }

        [SimpleField]
        public string entityType__ { get; set; }    // Since this applies to all business entities,  use a name that is unlikely to cause collisions with other properties
    }
}
