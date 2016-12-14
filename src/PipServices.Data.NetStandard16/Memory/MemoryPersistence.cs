using PipServices.Commons.Config;
using PipServices.Commons.Data;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Reflect;
using PipServices.Commons.Run;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Data.Memory
{
    public class MemoryPersistence<T, K> : IReferenceable, IConfigurable, IOpenable, IClosable, ICleanable,
        IWriter<T, K>, IGetter<T, K>, ISetter<T>, IQuerableReader<T>
        where T : IIdentifiable<K>
        where K : class
    {
        private const int _defaultMaxPageSize = 100;

        protected readonly string _typeName;
        protected CompositeLogger _logger = new CompositeLogger();

        protected int _maxPageSize = _defaultMaxPageSize;
        protected List<T> _entities = new List<T>();
        protected ILoader<T> _loader;
        protected ISaver<T> _saver;
        protected readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public MemoryPersistence()
            : this(null, null)
        { }

        protected MemoryPersistence(ILoader<T> loader, ISaver<T> saver)
        {
            _typeName = typeof(T).Name;
            _loader = loader;
            _saver = saver;
        }

        public void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
        }

        public virtual void Configure(ConfigParams config)
        {
            // Todo: Use connection and auth components
            _maxPageSize = config.GetAsIntegerWithDefault("max_page_size", _maxPageSize);
        }

        public async Task OpenAsync(string correlationId)
        {
            await LoadAsync(correlationId);
        }

        public async Task CloseAsync(string correlationId)
        {
            await SaveAsync(correlationId);
        }

        private Task LoadAsync(string correlationId)
        {
            if (_loader == null)
                return Task.Delay(0);
            
            _lock.EnterWriteLock();

            try
            {
                _entities = _loader.LoadAsync(correlationId).Result;
                _logger.Trace(correlationId, "Loaded {0} of {1}", _entities.Count, _typeName);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return Task.Delay(0);
        }

        public Task<List<T>> GetListByQueryAsync(string correlationId, string query, SortParams sort)
        {
            List<T> result;

            _lock.EnterReadLock();

            try
            {
                _logger.Trace(correlationId, "Retrieved {0} of {1}", _entities.Count, _typeName);
                result = new List<T>(_entities);
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return Task.FromResult(result);
        }

        public Task<T> GetOneByIdAsync(string correlationId, K id)
        {
            _lock.EnterReadLock();

            try
            {
                var item = _entities.FirstOrDefault(x => x.Id == id);

                if (item != null)
                    _logger.Trace(correlationId, "Retrieved {0} by {1}", item, id);
                else
                    _logger.Trace(correlationId, "Cannot find {0} by {1}", _typeName, id);

                return Task.FromResult(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task SaveAsync(string correlationId)
        {
    	    if (_saver == null)
                return Task.Delay(0);

            _lock.EnterWriteLock();

            try
            {
                var task = _saver.SaveAsync(correlationId, _entities);
                task.Wait();

                _logger.Trace(correlationId, "Saved {0} of {1}", _entities.Count, _typeName);

                return Task.Delay(0);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public async Task<T> CreateAsync(string correlationId, T entity)
        {
            var identifiable = entity as IStringIdentifiable;
            if (identifiable != null && entity.Id == null)
                ObjectWriter.SetProperty(entity, nameof(entity.Id), IdGenerator.NextLong());

            _lock.EnterWriteLock();

            try
            {
                _entities.Add(entity);

                _logger.Trace(correlationId, "Created {0}", entity);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return entity;
        }

        public async Task<T> SetAsync(string correlationId, T entity)
        {
            var identifiable = entity as IStringIdentifiable;
            if (identifiable != null && entity.Id == null)
                ObjectWriter.SetProperty(entity, nameof(entity.Id), IdGenerator.NextLong());

            _lock.EnterWriteLock();

            try
            {
                var index = _entities.FindIndex(x => x.Id == entity.Id);

                if (index < 0) _entities.Add(entity);
                else _entities[index] = entity;

                _logger.Trace(correlationId, "Set {0}", entity);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return entity;
        }

        public async Task<T> UpdateAsync(string correlationId, T entity)
        {
            _lock.EnterWriteLock();

            try
            {
                var index = _entities.FindIndex(x => x.Id.Equals(entity.Id));

                if (index < 0)
                    return default(T);

                _entities[index] = entity;

                _logger.Trace(correlationId, "Updated {0}", entity);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return entity;
        }

        public async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var entity = _entities.Find(x => x.Id == id);

            if (entity == null)
                return default(T);

            _lock.EnterWriteLock();

            try
            {
                _entities.Remove(entity);

                _logger.Trace(correlationId, "Deleted {0}", entity);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return await Task.FromResult(entity);
        }

        public Task ClearAsync(string correlationId)
        {
            _lock.EnterWriteLock();

            try
            {
                _entities = new List<T>();

                _logger.Trace(correlationId, "Cleared {0}", _typeName);

                return SaveAsync(correlationId);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

    }
}