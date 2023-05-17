using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCopilot.Vectorize.Options
{
    public class Redis
    {
        public string? ConnectionString { get; set; }
        public ILogger? Logger { get; set; }
    }
}
