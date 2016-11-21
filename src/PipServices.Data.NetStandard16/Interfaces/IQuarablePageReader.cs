using PipServices.Commons.Data;

namespace PipServices.Data.Interfaces
{
    public interface IQuarablePageReader<T>
        where T : class
    {
        DataPage<T> GetPageByQuery(string correlationId, string query, PagingParams paging, SortParams sort);
    }
}
