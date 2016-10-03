using PipServices.Commons.Data;

namespace PipServices.Data.Interfaces
{
    public interface IFilteredPageReader<T>
         where T : class
    {
        DataPage<T> GetPageByFilter(string correlationId, FilterParams filter, PagingParams paging, SortParams sort);
    }
}
