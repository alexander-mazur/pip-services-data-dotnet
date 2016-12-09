using PipServices.Commons.Config;
using PipServices.Commons.Data;
using PipServices.Data.Memory;

namespace PipServices.Data.File
{
    public class FilePersistence<T, K> : MemoryPersistence<T, K>
        where T : IIdentifiable<K>
        where K : class
    {
        protected readonly JsonFilePersister<T> _persister;

        public FilePersistence(JsonFilePersister<T> persister)
            : base(persister, persister)
        {
            _persister = persister;
        }

        public FilePersistence()
            : this(new JsonFilePersister<T>())
        { }

        public override void Configure(ConfigParams config)
        {
            base.Configure(config);
            _persister.Configure(config);
        }
    }
}