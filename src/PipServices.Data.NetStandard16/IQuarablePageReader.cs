using PipServices.Commons.Data;
using System.Threading.Tasks;

namespace PipServices.Data
{
    public interface IQuarablePageReader<T>
        where T : class
    {
        Task<DataPage<T>> GetPageByQueryAsync(string correlationId, string query, PagingParams paging, SortParams sort);
    }
}
