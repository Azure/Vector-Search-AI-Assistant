using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Search.Models
{
    
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string id { get; set; }
        public string categoryId { get; set; }
        public string categoryName { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public double price { get; set; }
        public List<Tag> tags { get; set; }
        public float[]? vector { get; set; }

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
    }

    public class ProductCategory
    {
        public string id { get; set; }
        public string type { get; set; }
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
        public string id { get; set; }
        public string name { get; set; }

        public Tag(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}
