using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorSearchAiAssistant.Service.Interfaces
{
    public interface IRAGService
    {
        Task<string> GetResponse(string userPrompt);
    }
}
