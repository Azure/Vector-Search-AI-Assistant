namespace DataCopilot.Vectorize.Models
{
    public class SalesOrder
    {
        
        public string id { get; set; }
        public string type { get; set; }
        public string customerId { get; set; }
        public string orderDate { get; set; }
        public string shipDate { get; set; }
        public List<SalesOrderDetails> details { get; set; }

        public SalesOrder(string id, string type, string customerId, string orderDate, string shipDate, List<SalesOrderDetails> details)
        {
            this.id = id;
            this.type = type;
            this.customerId = customerId;
            this.orderDate = orderDate;
            this.shipDate = shipDate;
            this.details = details;
        }
    }

    public class SalesOrderDetails
    {
        public string sku { get; set; }
        public string name { get; set; }
        public double price { get; set; }
        public int quantity { get; set; }

        public SalesOrderDetails(string sku, string name, double price, int quantity)
        {
            this.sku = sku;
            this.name = name;
            this.price = price;
            this.quantity = quantity;
        }
    }
}
