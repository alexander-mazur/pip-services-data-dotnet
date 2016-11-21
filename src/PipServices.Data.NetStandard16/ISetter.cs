using System.Threading.Tasks;

namespace PipServices.Data
{
    public interface ISetter<T>
    {
        Task<T> SetAsync(string correlationId, T entity);
    }
}
