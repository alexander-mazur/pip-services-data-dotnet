using System;
using System.Threading.Tasks;

namespace PipServices.Data.Interfaces
{
    public interface ISetter<T>
    {
        Task<T> SetAsync(string correlationId, T entity);
    }
}
