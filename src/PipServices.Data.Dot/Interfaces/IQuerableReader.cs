using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PipServices.Commons.Data;

namespace PipServices.Data.Interfaces
{
    public interface IQuerableReader<T>
    {
        Task<IEnumerable<T>> GetListByFilterAsync(string correlationId, string query, SortParams sort, CancellationToken token);
    }
}
