using System;
using System.Threading;
using PipServices.Data.MongoDb;
using PipServices.Commons.Config;
using Xunit;

namespace PipServices.Data.Test.MongoDb
{
    public sealed class MongoDbPersistenceTest : IDisposable
    {
        private static MongoDbPersistence<PersistenceFixture.Dummy, string> Db { get; } 
            = new MongoDbPersistence<PersistenceFixture.Dummy, string>("dummies");
        private static PersistenceFixture Fixture { get; set; }

        private PersistenceFixture GetFixture()
        {
            return new PersistenceFixture(Db, Db, Db, Db, Db, Db, Db, Db);
        }

        public MongoDbPersistenceTest()
        {
            if (Db == null)
                return;

            Db.Configure(ConfigParams.FromTuples(
                "connection.type", "mongodb",
                "connection.database", "test",
                "connection.uri", ""));

            var task = Db.OpenAsync(null);
            task.Wait();

            task = Db.ClearAsync(null);
            task.Wait();

            Fixture = GetFixture();
        }

        [Fact]
        public void TestCrudOperations()
        {
            if (Fixture == null) return;

            var task = Fixture.TestCrudOperationsAsync();
            task.Wait();
        }

        [Fact]
        public void TestMultithreading()
        {
            if (Fixture == null) return;

            var task = Fixture.TestMultithreading();
            task.Wait();
        }

        public void Dispose()
        {
            if (Db == null)
                return;

            var task = Db.CloseAsync(null);
            task.Wait();
        }
    }
}
