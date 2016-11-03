using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Data.Interfaces
{
    public interface IWriter<T, in TI>
    {
        Task<T> CreateAsync(string correlationId, T entity, CancellationToken token);
        Task<T> UpdateAsync(string correlationId, T entity, CancellationToken token);
        Task<T> DeleteByIdAsync(string correlationId, TI id, CancellationToken token);
    }
}
