using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vectorize.Options
{
    public record OpenAi
    {
        public string? Endpoint { get; set; }

        public string? Key { get; set; }

        public string? EmbeddingsDeployment { get; set; }

        public string? MaxTokens { get; set; }

        public ILogger? Logger { get; set; }
    }
}
