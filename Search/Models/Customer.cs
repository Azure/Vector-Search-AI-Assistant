using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Search.Models
{
    public class Customer
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
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
        public float[]? vector { get; set; }

        public Customer(string id, string type, string customerId, string title, 
            string firstName, string lastName, string emailAddress, string phoneNumber, 
            string creationDate, List<CustomerAddress> addresses, Password password,
            int salesOrderCount, float[]? vector = null)
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
        public string hash { get; set; }
        public string salt { get; set; }

        public Password(string hash, string salt)
        {
            this.hash = hash;
            this.salt = salt;
        }
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
        public string type { get; set; }
        public List<float> coordinates { get; set; }

       public Location(string type, List<float> coordinates)
        {
            this.type = type;
            this.coordinates = coordinates;
        }
    }
}
