using System.Threading;
using System.Threading.Tasks;
using PipServices.Commons.Data;

namespace PipServices.Data.Interfaces
{
    public interface IGetter<T, in TI>
        where T : IIdentifiable<TI>
        where TI : class
    {
        Task<T> GetOneByIdAsync(string correlationId, TI id, CancellationToken token);
    }
}
