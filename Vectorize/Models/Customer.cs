namespace DataCopilot.Vectorize.Models
{
    public class Customer
    {
        public string id { get; set; }
        public string type { get; set; }
        public string customerId { get; set; }
        public string title { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string emailAddress { get; set; }
        public string phoneNumber { get; set; }
        public string creationDate { get; set; }
        public List<CustomerAddress> addresses { get; set; }
        public Password password { get; set; }
        public int salesOrderCount { get; set; }
    }

    public class Password
    {
        public string hash { get; set; }
        public string salt { get; set; }
    }

    public class CustomerAddress
    {
        public string addressLine1 { get; set; }
        public string addressLine2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string zipCode { get; set; }
        public Location location { get; set; }
    }

    public class Location
    {
        public string type { get; set; }
        public List<float> coordinates { get; set; }
    }
}
