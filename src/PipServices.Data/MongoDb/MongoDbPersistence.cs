using System;
using System.Threading;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Data;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Data.Interfaces;

namespace PipServices.Data.MongoDb
{
    public class MongoDbPersistence<T, TI> : IReferenceable, IConfigurable, IOpenable, IClosable, ICleanable,
        IWriter<T, TI>, IGetter<T, TI>, ISetter<T>
        where T : IIdentifiable<TI>
        where TI : class
    {
        public Task ClearAsync(string correlationId)
        {
            throw new NotImplementedException();
        }

        public Task CloseAsync(string correlationId)
        {
            throw new NotImplementedException();
        }

        public void Configure(ConfigParams config)
        {
            throw new NotImplementedException();
        }

        public T GetOneById(string correlationId, TI id)
        {
            throw new NotImplementedException();
        }

        public Task OpenAsync(string correlationId, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public void SetReferences(IReferences references)
        {
            throw new NotImplementedException();
        }

        public Task<T> SetAsync(string correlationId, T entity)
        {
            throw new NotImplementedException();
        }

        public Task<T> CreateAsync(string correlationId, T entity)
        {
            throw new NotImplementedException();
        }

        public Task<T> DeleteByIdAsync(string correlationId, TI id)
        {
            throw new NotImplementedException();
        }

        public Task<T> UpdateAsync(string correlationId, T entity)
        {
            throw new NotImplementedException();
        }
    }
}