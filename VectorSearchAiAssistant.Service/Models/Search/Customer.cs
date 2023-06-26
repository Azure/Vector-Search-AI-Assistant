using Azure.Search.Documents.Indexes;

namespace VectorSearchAiAssistant.Service.Models.Search
{
    public class Customer : EmbeddedEntity
    {
        [SearchableField(IsKey = true, IsFilterable = true)]
        public string id { get; set; }
        [SimpleField]
        public string type { get; set; }
        [SimpleField]
        public string customerId { get; set; }
        [SimpleField]
        public string title { get; set; }
        [SimpleField]
        public string firstName { get; set; }
        [SimpleField]
        public string lastName { get; set; }
        [SimpleField]
        public string emailAddress { get; set; }
        [SimpleField]
        public string phoneNumber { get; set; }
        [SimpleField]
        public string creationDate { get; set; }
        [SimpleField]
        public List<CustomerAddress> addresses { get; set; }
        [SimpleField(IsHidden = true)]
        public Password password { get; set; }
        [SimpleField]
        public double salesOrderCount { get; set; }

        public Customer(string id, string type, string customerId, string title,
            string firstName, string lastName, string emailAddress, string phoneNumber,
            string creationDate, List<CustomerAddress> addresses, Password password,
            double salesOrderCount, float[]? vector = null)
        {
            this.id = id;
            this.type = type;
            this.customerId = customerId;
            this.title = title;
            this.firstName = firstName;
            this.lastName = lastName;
            this.emailAddress = emailAddress;
            this.phoneNumber = phoneNumber;
            this.creationDate = creationDate;
            this.addresses = addresses;
            this.password = password;
            this.salesOrderCount = salesOrderCount;
            this.vector = vector;
        }
    }

    public class Password
    {
        [SimpleField(IsHidden = true)]
        public string hash { get; set; }
        [SimpleField(IsHidden = true)]
        public string salt { get; set; }

        public Password(string hash, string salt)
        {
            this.hash = hash;
            this.salt = salt;
        }
    }

    public class CustomerAddress
    {
        [SimpleField]
        public string addressLine1 { get; set; }
        [SimpleField]
        public string addressLine2 { get; set; }
        [SimpleField]
        public string city { get; set; }
        [SimpleField]
        public string state { get; set; }
        [SimpleField]
        public string country { get; set; }
        [SimpleField]
        public string zipCode { get; set; }
        [SimpleField]
        public Location location { get; set; }

        public CustomerAddress(string addressLine1, string addressLine2, string city, string state, string country, string zipCode, Location location)
        {
            this.addressLine1 = addressLine1;
            this.addressLine2 = addressLine2;
            this.city = city;
            this.state = state;
            this.country = country;
            this.zipCode = zipCode;
            this.location = location;
        }
    }

    public class Location
    {
        [SimpleField]
        public string type { get; set; }
        [FieldBuilderIgnore]
        public List<float> coordinates { get; set; }

        public Location(string type, List<float> coordinates)
        {
            this.type = type;
            this.coordinates = coordinates;
        }
    }
}
