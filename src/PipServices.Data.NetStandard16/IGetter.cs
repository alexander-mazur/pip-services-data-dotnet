using System.Threading;
using System.Threading.Tasks;
using PipServices.Commons.Data;

namespace PipServices.Data
{
    public interface IGetter<T, in K>
        where T : IIdentifiable<K>
        where K : class
    {
        Task<T> GetOneByIdAsync(string correlationId, K id);
    }
}
