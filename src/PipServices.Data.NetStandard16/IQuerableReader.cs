using PipServices.Commons.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices.Data
{
    public interface IQuerableReader<T>
    {
        Task<List<T>> GetListByQueryAsync(string correlationId, string query, SortParams sort);
    }
}
