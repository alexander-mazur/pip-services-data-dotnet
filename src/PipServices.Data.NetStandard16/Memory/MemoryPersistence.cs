using PipServices.Commons.Config;
using PipServices.Commons.Data;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private const int DefaultMaxPageSize = 100;

        protected readonly string TypeName;

        protected ILogger Logger = new NullLogger();

        protected int MaxPageSize = DefaultMaxPageSize;
        protected ImmutableList<T> Items = ImmutableList.Create<T>();
        private readonly ILoader<T> _loader;
        private readonly ISaver<T> _saver;

        protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public MemoryPersistence()
            : this(null, null)
        {
        }

        protected MemoryPersistence(ILoader<T> loader, ISaver<T> saver)
        {
            TypeName = typeof(T).Name;
            _loader = loader;
            _saver = saver;
        }

        public void SetReferences(IReferences references)
        {
            // Todo: Use composite logger
            var logger = (ILogger)references.GetOneOptional(new Descriptor("*", "logger", "*", "*"));

            Logger = logger ?? Logger;
        }

        public virtual void Configure(ConfigParams config)
        {
            // Todo: Use connection and auth components
            MaxPageSize = config.GetAsIntegerWithDefault("max_page_size", DefaultMaxPageSize);
        }

        public Task OpenAsync(string correlationId)
        {
            return LoadAsync(correlationId);
        }

        public Task CloseAsync(string correlationId)
        {
            return SaveAsync(correlationId);
        }

        private Task LoadAsync(string correlationId)
        {
            if (_loader == null)
                return Task.Delay(0);
            
            Lock.EnterWriteLock();

            try
            {
                var task = _loader.LoadAsync(correlationId);
                task.Wait();

                var loadedItems = task.Result;

                Items = ImmutableList.CreateRange(loadedItems);

                Logger.Trace(correlationId, "Loaded {0} of {1}", Items.Count, TypeName);

                return Task.Delay(0);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public Task<List<T>> GetListByQueryAsync(string correlationId, string query, SortParams sort)
        {
            Lock.EnterReadLock();

            try
            {
                Logger.Trace(correlationId, "Retrieved {0} of {1}", Items.Count, TypeName);

                return Task.FromResult(new List<T>(Items));
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }

        public Task<T> GetOneByIdAsync(string correlationId, K id)
        {
            Lock.EnterReadLock();

            try
            {
                var item = Items.FirstOrDefault(x => x.Id == id);

                if (item != null)
                    Logger.Trace(correlationId, "Retrieved {0} by {1}", item, id);
                else
                    Logger.Trace(correlationId, "Cannot find {0} by {1}", TypeName, id);

                return Task.FromResult(item);
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }

        public Task SaveAsync(string correlationId)
        {
    	    if (_saver == null)
                return Task.Delay(0);

            Lock.EnterWriteLock();

            try
            {
                var task = _saver.SaveAsync(correlationId, Items);
                task.Wait();

                Logger.Trace(correlationId, "Saved {0} of {1}", Items.Count, TypeName);

                return Task.Delay(0);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public async Task<T> CreateAsync(string correlationId, T entity)
        {
            var identifiable = entity as IStringIdentifiable;
            if (identifiable != null && entity.Id == null)
    		    identifiable.Id = IdGenerator.NextLong();

            Lock.EnterWriteLock();

            try
            {
                Items = Items.Add(entity);

                Logger.Trace(correlationId, "Created {0}", entity);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return entity;
        }

        public async Task<T> SetAsync(string correlationId, T entity)
        {
            var identifiable = entity as IStringIdentifiable;
            if (identifiable != null && entity.Id == null)
                identifiable.Id = IdGenerator.NextLong();

            Lock.EnterWriteLock();

            try
            {
                var item = Items.Find(x => x.Id == entity.Id);

                Items = item == null ? Items.Add(entity) : Items.Replace(item, entity);

                Logger.Trace(correlationId, "Set {0}", entity);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return entity;
        }

        public async Task<T> UpdateAsync(string correlationId, T entity)
        {
            Lock.EnterWriteLock();

            try
            {
                var oldEntity = Items.Find(x => x.Id == entity.Id);

                if (oldEntity == null)
                    return default(T);

                Items = Items.Replace(oldEntity, entity);

                Logger.Trace(correlationId, "Updated {0}", entity);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return entity;
        }

        public async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var entity = Items.Find(x => x.Id == id);

            if (entity == null)
                return default(T);

            Lock.EnterWriteLock();

            try
            {
                Items = Items.Remove(entity);

                Logger.Trace(correlationId, "Deleted {0}", entity);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return await Task.FromResult(entity);
        }

        public Task ClearAsync(string correlationId)
        {
            Lock.EnterWriteLock();

            try
            {
                Items = ImmutableList.Create<T>();

                Logger.Trace(correlationId, "Cleared {0}", TypeName);

                return SaveAsync(correlationId);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

    }
}