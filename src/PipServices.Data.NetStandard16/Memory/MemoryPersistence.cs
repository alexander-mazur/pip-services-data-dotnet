using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Data.Memory
{
    public class MemoryPersistence<T> : IReferenceable, IOpenable, ICleanable
    {
        protected readonly string _typeName;
        protected CompositeLogger _logger = new CompositeLogger();

        protected List<T> _items = new List<T>();
        protected ILoader<T> _loader;
        protected ISaver<T> _saver;
        protected readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        protected bool _opened = false;

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

        public bool IsOpened()
        {
            return _opened;
        }

        public async Task OpenAsync(string correlationId)
        {
            await LoadAsync(correlationId);
            _opened = true;
        }

        public async Task CloseAsync(string correlationId)
        {
            await SaveAsync(correlationId);
            _opened = false;
        }

        private Task LoadAsync(string correlationId)
        {
            if (_loader == null)
                return Task.Delay(0);
            
            _lock.EnterWriteLock();

            try
            {
                _items = _loader.LoadAsync(correlationId).Result;
                _logger.Trace(correlationId, "Loaded {0} of {1}", _items.Count, _typeName);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return Task.Delay(0);
        }

        public Task SaveAsync(string correlationId)
        {
    	    if (_saver == null)
                return Task.Delay(0);

            _lock.EnterWriteLock();

            try
            {
                var task = _saver.SaveAsync(correlationId, _items);
                task.Wait();

                _logger.Trace(correlationId, "Saved {0} of {1}", _items.Count, _typeName);

                return Task.Delay(0);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Task ClearAsync(string correlationId)
        {
            _lock.EnterWriteLock();

            try
            {
                _items = new List<T>();

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