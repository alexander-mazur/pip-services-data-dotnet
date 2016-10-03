using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Data;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Data.Interfaces;

namespace PipServices.Data.Memory
{
    public class MemoryPersistence<T, TI> : IReferenceable, IConfigurable, IOpenable, IClosable, ICleanable,
        IWriter<T, TI>, IGetter<T, TI>, ISetter<T>
        where T : IIdentifiable<TI>
        where TI : class
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

        public async Task ClearAsync(string correlationId)
        {
            Lock.EnterWriteLock();

            try
            {
                Items = ImmutableList.Create<T>();

                Logger.Trace(correlationId, "Cleared {0}", TypeName);

                await SaveAsync(correlationId);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public Task CloseAsync(string correlationId)
        {
            return SaveAsync(correlationId);
        }

        public virtual void Configure(ConfigParams config)
        {
            MaxPageSize = config.GetAsIntegerWithDefault("max_page_size", DefaultMaxPageSize);
        }

        public T GetOneById(string correlationId, TI id)
        {
            Lock.EnterReadLock();

            try
            {
                var item = Items.FirstOrDefault(x => x.Id == id);

                if (item != null)
                    Logger.Trace(correlationId, "Retrieved {0} by {1}", item, id);
                else
                    Logger.Trace(correlationId, "Cannot find {0} by {1}", TypeName, id);

                return item;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public Task OpenAsync(string correlationId, CancellationToken token)
        {
            return LoadAsync(correlationId);
        }

        private async Task LoadAsync(string correlationId)
        {
            if (_loader == null)
                return;

            Lock.EnterWriteLock();

            try
            {
                var loadedItems = await _loader.LoadAsync(correlationId);

                Items = ImmutableList.CreateRange(loadedItems);

                Logger.Trace(correlationId, "Loaded {0} of {1}", Items.Count, TypeName);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public async Task SaveAsync(string correlationId)
        {
    	    if (_saver == null)
                return;

            Lock.EnterWriteLock();

            try
            {
                await _saver.SaveAsync(correlationId, Items);

                Logger.Trace(correlationId, "Saved {0} of {1}", Items.Count, TypeName);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public void SetReferences(IReferences references)
        {
            var logger = (ILogger)references.GetOneOptional(new Descriptor("*", "logger", "*", "*"));

            Logger = logger ?? Logger;
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

        public async Task<T> DeleteByIdAsync(string correlationId, TI id)
        {
            Lock.EnterUpgradeableReadLock();

            T entity;

            try
            {
                entity = Items.Find(x => x.Id == id);

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
            }
            finally
            {
                Lock.ExitUpgradeableReadLock();
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

                Logger.Trace(correlationId, "Update {0}", entity);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            await SaveAsync(correlationId);

            return entity;
        }
    }
}