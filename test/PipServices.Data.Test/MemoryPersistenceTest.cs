using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipServices.Data.Memory;
using PipServices.Commons.Config;
using Xunit;

namespace PipServices.Data.Test
{
    public sealed class MemoryPersistenceTest
    {
        private static MemoryPersistence<PersistenceFixture.Dummy, string> Db { get; } = new MemoryPersistence<PersistenceFixture.Dummy, string>();
        private static PersistenceFixture Fixture { get; set; }

        private PersistenceFixture GetFixture()
        {
            return new PersistenceFixture(Db, Db, Db, Db, Db, Db, Db, Db);
        }

        public MemoryPersistenceTest()
        {
            if (Db == null)
                return;

            Db.Configure(new ConfigParams());

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

        //[Fact]
        public void TestMultithreading()
        {
            if (Fixture == null) return;

            var task = Fixture.TestMultithreading(CancellationToken.None);
            task.Wait();
        }
    }
}
