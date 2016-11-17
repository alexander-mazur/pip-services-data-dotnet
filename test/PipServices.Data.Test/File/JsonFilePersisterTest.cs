using PipServices.Data.File;
using PipServices.Commons.Errors;
using PipServices.Commons.Config;
using Xunit;

namespace PipServices.Data.Test.File
{
    public sealed class JsonFilePersisterTest
    {
        private readonly JsonFilePersister<PersistenceFixture.Dummy> _persister;

        public JsonFilePersisterTest()
        {
            _persister = new JsonFilePersister<PersistenceFixture.Dummy>();
        }

        [Fact]
        public void Configure_IfNoPathKey_Fails()
        {
            Assert.Throws<ConfigException>(() => _persister.Configure(new ConfigParams()));
        }

        [Fact]
        public void Configure_IfPathKeyCheckProperty_IsOk()
        {
            const string fileName = nameof(JsonFilePersisterTest);

            _persister.Configure(ConfigParams.FromTuples("path", fileName));

            Assert.Equal(fileName, _persister.Path);
        }

        public void LoadAsync_()
        {

        }

        public void SaveAsync_()
        {

        }
    }
}
