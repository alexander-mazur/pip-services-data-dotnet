using PipServices.Commons.Data;
using System.Threading.Tasks;

namespace PipServices.Data
{
    public interface IFilteredPageReader<T>
         where T : class
    {
        Task<DataPage<T>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort);
    }
}
