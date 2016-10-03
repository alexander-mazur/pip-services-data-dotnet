using PipServices.Commons.Config;
using PipServices.Commons.Data;
using PipServices.Data.Memory;

namespace PipServices.Data.File
{
    public class FilePersistence<T, TI> : MemoryPersistence<T, TI>
        where T : IIdentifiable<TI>
        where TI : class
    {
        protected readonly JsonFilePersister<T> Persister;

        private FilePersistence(JsonFilePersister<T> persister)
            : base(persister, persister)
        {
            Persister = persister;
        }

        public FilePersistence()
            : this(new JsonFilePersister<T>())
        {
        }

        public override void Configure(ConfigParams config)
        {
            base.Configure(config);

            Persister.Configure(config);
        }
    }
}