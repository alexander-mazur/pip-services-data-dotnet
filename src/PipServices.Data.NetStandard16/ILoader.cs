using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices.Data
{
    public interface ILoader<T>
    {
        Task<List<T>> LoadAsync(string correlationId);
    }
}
