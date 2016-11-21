using System.Threading;
using PipServices.Commons.Config;
using PipServices.Data.File;
using Xunit;

namespace PipServices.Data.Test.File
{
    public sealed class FilePersistenceTest
    {
        private static FilePersistence<PersistenceFixture.Dummy, string> Db { get; } = new FilePersistence<PersistenceFixture.Dummy,string>(new JsonFilePersister<PersistenceFixture.Dummy>());
        private static PersistenceFixture Fixture { get; set; }

        private PersistenceFixture GetFixture()
        {
            return new PersistenceFixture(Db, Db, Db, Db, Db, Db, Db, Db);
        }

        public FilePersistenceTest()
        {
            if (Db == null)
                return;

            Db.Configure(ConfigParams.FromTuples("path", nameof(FilePersistenceTest)));

            var task = Db.OpenAsync(null, CancellationToken.None);
            task.Wait();

            task = Db.ClearAsync(null, CancellationToken.None);
            task.Wait();

            Fixture = GetFixture();
        }

        [Fact]
        public void TestCrudOperations()
        {
            if (Fixture == null) return;

            var task = Fixture.TestCrudOperationsAsync(CancellationToken.None);
            task.Wait();
        }

        [Fact]
        public void TestMultithreading()
        {
            if (Fixture == null) return;

            var task = Fixture.TestMultithreading(CancellationToken.None);
            task.Wait();
        }
    }
}
