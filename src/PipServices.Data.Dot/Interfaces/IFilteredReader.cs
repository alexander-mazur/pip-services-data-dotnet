using System.Collections.Generic;
using PipServices.Commons.Data;

namespace PipServices.Data.Interfaces
{
    public interface IFilteredReader<out T>
        where T : class 
    {
        IEnumerable<T> GetListByFilter(string correlationId, FilterParams filter, PagingParams paging, SortParams sort);
    }
}
