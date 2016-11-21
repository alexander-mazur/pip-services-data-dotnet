using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices.Data
{
    public interface ISaver<in T>
    {
        Task SaveAsync(string correlationId, IEnumerable<T> entities);
    }
}
