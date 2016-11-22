using System.Collections.Generic;
using PipServices.Commons.Data;
using System.Threading.Tasks;

namespace PipServices.Data
{
    public interface IFilteredReader<T>
        where T : class 
    {
        Task<List<T>> GetListByFilterAsync(string correlationId, FilterParams filter, SortParams sort);
    }
}
