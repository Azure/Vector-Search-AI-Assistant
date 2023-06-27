using Azure.Search.Documents.Indexes;

namespace VectorSearchAiAssistant.Service.Models.Search
{
    public class SalesOrder : EmbeddedEntity
    {
        [SearchableField(IsKey = true, IsFilterable = true)]
        public string id { get; set; }
        [SimpleField]
        public string type { get; set; }
        [SimpleField]
        public string customerId { get; set; }
        [SimpleField]
        public string orderDate { get; set; }
        [SimpleField]
        public string shipDate { get; set; }
        [SimpleField]
        public List<SalesOrderDetails> details { get; set; }

        public SalesOrder(string id, string type, string customerId, string orderDate, string shipDate, List<SalesOrderDetails> details, float[]? vector = null)
        {
            this.id = id;
            this.type = type;
            this.customerId = customerId;
            this.orderDate = orderDate;
            this.shipDate = shipDate;
            this.details = details;
            this.vector = vector;
        }
    }

    public class SalesOrderDetails
    {
        [SimpleField]
        public string sku { get; set; }
        [SimpleField]
        public string name { get; set; }
        [SimpleField]
        public double price { get; set; }
        [SimpleField]
        public double quantity { get; set; }

        public SalesOrderDetails(string sku, string name, double price, double quantity)
        {
            this.sku = sku;
            this.name = name;
            this.price = price;
            this.quantity = quantity;
        }
    }
}
