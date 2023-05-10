using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vectorize.Options
{
    public record MongoDb
    {
        public string? Connection { get; set; }
        public string? DatabaseName { get; set; } 

        public string? CollectionName { get; set; }

        public ILogger? Logger { get; set; }

    }
}
