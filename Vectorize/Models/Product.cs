namespace DataCopilot.Vectorize.Models
{
    public class Product
    {
        public string id { get; set; }
        public string categoryId { get; set; }
        public string categoryName { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public double price { get; set; }
        public List<Tag> tags { get; set; }
    }

    public class ProductCategory
    {
        public string id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
    }

    public class Tag
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}
