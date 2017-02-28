using PipServices.Commons.Config;
using PipServices.Commons.Data;
using PipServices.Commons.Reflect;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PipServices.Data.Memory
{
    public class IdentifiableMemoryPersistence<T, K> : MemoryPersistence<T>, IReconfigurable,
        IWriter<T, K>, IGetter<T, K>, ISetter<T>, IQuerableReader<T>
        where T : IIdentifiable<K>
        where K : class
    {
        private const int _defaultMaxPageSize = 100;

        protected int _maxPageSize = _defaultMaxPageSize;

        public IdentifiableMemoryPersistence()
            : this(null, null)
        { }

        protected IdentifiableMemoryPersistence(ILoader<T> loader, ISaver<T> saver)
            : base(loader, saver)
        { }

        public virtual void Configure(ConfigParams config)
        {
            // Todo: Use connection and auth components
            _maxPageSize = config.GetAsIntegerWithDefault("max_page_size", _maxPageSize);
        }

        public Task<List<T>> GetListByQueryAsync(string correlationId, string query, SortParams sort)
        {
            List<T> result;

            _lock.EnterReadLock();

            try
            {
                _logger.Trace(correlationId, "Retrieved {0} of {1}", _items.Count, _typeName);
                result = new List<T>(_items);
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
                var item = _items.FirstOrDefault(x => x.Id == id);

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

        public async Task<T> CreateAsync(string correlationId, T item)
        {
            var identifiable = item as IStringIdentifiable;
            if (identifiable != null && item.Id == null)
                ObjectWriter.SetProperty(item, nameof(item.Id), IdGenerator.NextLong());

            _lock.EnterWriteLock();

            try
            {
                _items.Add(item);

                _logger.Trace(correlationId, "Created {0}", item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return item;
        }

        public async Task<T> SetAsync(string correlationId, T item)
        {
            var identifiable = item as IStringIdentifiable;
            if (identifiable != null && item.Id == null)
                ObjectWriter.SetProperty(item, nameof(item.Id), IdGenerator.NextLong());

            _lock.EnterWriteLock();

            try
            {
                var index = _items.FindIndex(x => x.Id == item.Id);

                if (index < 0) _items.Add(item);
                else _items[index] = item;

                _logger.Trace(correlationId, "Set {0}", item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return item;
        }

        public async Task<T> UpdateAsync(string correlationId, T item)
        {
            _lock.EnterWriteLock();

            try
            {
                var index = _items.FindIndex(x => x.Id.Equals(item.Id));

                if (index < 0)
                    return default(T);

                _items[index] = item;

                _logger.Trace(correlationId, "Updated {0}", item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return item;
        }

        public async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var item = _items.Find(x => x.Id == id);

            if (item == null)
                return default(T);

            _lock.EnterWriteLock();

            try
            {
                _items.Remove(item);

                _logger.Trace(correlationId, "Deleted {0}", item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return await Task.FromResult(item);
        }

    }
}