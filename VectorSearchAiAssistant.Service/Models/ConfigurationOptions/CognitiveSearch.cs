using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorSearchAiAssistant.Service.Models.ConfigurationOptions
{
    public record CognitiveSearch
    {
        public required string Endpoint { get; init; }

        public required string AdminKey { get; init; }

        public required string IndexName { get; init; }

        public required string MaxVectorSearchResults { get; init; }

        public ILogger? Logger { get; init; }
    }
}
