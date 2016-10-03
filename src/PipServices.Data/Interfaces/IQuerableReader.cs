using System.Collections.Generic;
using PipServices.Commons.Data;

namespace PipServices.Data.Interfaces
{
    public interface IQuerableReader<out T>
        where T : class
    {
        IEnumerable<T> GetListByFilter(string correlationId, string query, PagingParams paging, SortParams sort);
    }
}
