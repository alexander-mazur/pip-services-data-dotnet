using PipServices.Commons.Data;
using System.Threading.Tasks;

namespace PipServices.Data
{
    public interface IDynamicWriter<T, in K>
        where T : class
        where K : class
    {
        Task<T> CreateAsync(string correlationId, AnyValueMap entityData);
        Task<T> UpdateAsync(string correlationId, K id, AnyValueMap entityData);
        Task<T> DeleteByIdAsync(string correlationId, K id);
    }
}
