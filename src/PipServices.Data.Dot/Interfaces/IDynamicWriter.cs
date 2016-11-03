using PipServices.Commons.Data;

namespace PipServices.Data.Interfaces
{
    public interface IDynamicWriter<out T, in TI>
        where T : class
        where TI : class
    {
        T Create(string correlationId, AnyValueMap entityData);
        T Update(string correlationId, TI id, AnyValueMap entityData);
        T DeleteById(string correlationId, TI id);
    }
}
