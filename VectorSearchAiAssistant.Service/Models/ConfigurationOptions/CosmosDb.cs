using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record CosmosDb
    {
        public required string Endpoint { get; init; }

        public required string Key { get; init; }

        public required string Database { get; init; }

        public required string Containers { get; init; }

        public ILogger? Logger { get; init; }
    }
}
