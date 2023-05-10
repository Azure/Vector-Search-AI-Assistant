using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Models
{
    public record DocumentVector
    {
        public string id { get; set; }
        public string itemId { get; set; }
        public string partitionKey { get; set; }
        public string containerName { get; set; }
        public float[]? vector { get; set; }

        public DocumentVector(string itemId, string partitionKey, string containerName, float[]? vector = null)
        {
            id = Guid.NewGuid().ToString();
            this.itemId = itemId;
            this.partitionKey = partitionKey;
            this.containerName = containerName;
            this.vector = vector;
        }
    }
}
