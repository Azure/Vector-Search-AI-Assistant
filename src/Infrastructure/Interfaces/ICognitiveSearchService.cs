using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace BuildYourOwnCopilot.Service.Interfaces
{
    public interface IAISearchService
    {
        Task Initialize(List<Type> typesToIndex);

        Task IndexItem(object item);

        Task<Response<SearchResults<SearchDocument>>> SearchAsync(SearchOptions searchOptions);
    }
}
