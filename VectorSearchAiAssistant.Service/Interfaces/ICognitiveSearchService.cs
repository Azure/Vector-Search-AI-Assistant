using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorSearchAiAssistant.Service.Interfaces
{
    public interface ICognitiveSearchService
    {
        Task Initialize(List<Type> typesToIndex);

        Task IndexItem(object item);
    }
}
