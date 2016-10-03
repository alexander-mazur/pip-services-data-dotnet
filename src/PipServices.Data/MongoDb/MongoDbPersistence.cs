using System;
using System.Collections.Generic;
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
        public void SetReferences(IReferences references)
        {
            throw new NotImplementedException();
        }

        public void Configure(ConfigParams config)
        {
            throw new NotImplementedException();
        }

        public Task OpenAsync(string correlationId, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task CloseAsync(string correlationId, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task ClearAsync(string correlationId, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<T> CreateAsync(string correlationId, T entity, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<T> UpdateAsync(string correlationId, T entity, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<T> DeleteByIdAsync(string correlationId, TI id, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetOneByIdAsync(string correlationId, TI id, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> GetAllAsync(string correlationId, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<T> SetAsync(string correlationId, T entity, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}