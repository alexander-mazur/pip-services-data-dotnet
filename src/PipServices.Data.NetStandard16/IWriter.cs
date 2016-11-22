using System.Threading.Tasks;

namespace PipServices.Data
{
    public interface IWriter<T, in K>
    {
        Task<T> CreateAsync(string correlationId, T entity);
        Task<T> UpdateAsync(string correlationId, T entity);
        Task<T> DeleteByIdAsync(string correlationId, K id);
    }
}
