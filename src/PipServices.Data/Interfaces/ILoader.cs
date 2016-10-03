using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices.Data.Interfaces
{
    public interface ILoader<T>
    {
        Task<IEnumerable<T>> LoadAsync(string correlationId);
    }
}
