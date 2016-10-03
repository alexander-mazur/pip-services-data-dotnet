using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Data.Interfaces
{
    public interface ICleanable
    {
        Task ClearAsync(string correlationId, CancellationToken token);
    }
}
