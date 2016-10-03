using PipServices.Commons.Data;

namespace PipServices.Data.Interfaces
{
    public interface IGetter<out T, in TI>
        where T : IIdentifiable<TI>
        where TI : class
    {
        T GetOneById(string correlationId, TI id);
    }
}
