using Azure.Search.Documents.Indexes;

namespace VectorSearchAiAssistant.Service.Models.Search
{

    public class Product : EmbeddedEntity
    {
        [SearchableField(IsKey = true, IsFilterable = true)]
        public string id { get; set; }
        [SimpleField]
        public string categoryId { get; set; }
        [SimpleField]
        public string categoryName { get; set; }
        [SimpleField]
        public string sku { get; set; }
        [SimpleField]
        public string name { get; set; }
        [SimpleField]
        public string description { get; set; }
        [SimpleField]
        public double price { get; set; }
        [SimpleField]
        public List<Tag> tags { get; set; }

        public Product(string id, string categoryId, string categoryName, string sku, string name, string description, double price, List<Tag> tags, float[]? vector = null)
        {
            this.id = id;
            this.categoryId = categoryId;
            this.categoryName = categoryName;
            this.sku = sku;
            this.name = name;
            this.description = description;
            this.price = price;
            this.tags = tags;
            this.vector = vector;
        }

        public Product()
        {
        }
    }

    public class ProductCategory
    {
        [SimpleField]
        public string id { get; set; }
        [SimpleField]
        public string type { get; set; }
        [SimpleField]
        public string name { get; set; }

        public ProductCategory(string id, string type, string name)
        {
            this.id = id;
            this.type = type;
            this.name = name;
        }
    }

    public class Tag
    {
        [SimpleField]
        public string id { get; set; }
        [SimpleField]
        public string name { get; set; }

        public Tag(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}
