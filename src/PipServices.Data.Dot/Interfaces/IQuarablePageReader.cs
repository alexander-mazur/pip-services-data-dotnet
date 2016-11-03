using PipServices.Commons.Data;

namespace PipServices.Data.Interfaces
{
    public interface IQuarablePageReader<T>
        where T : class
    {
        DataPage<T> GetPageByFilter(string correlationId, string query, PagingParams paging, SortParams sort);
    }
}
