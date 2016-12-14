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

        public MongoDbPersistenceTest()
        {
            if (Db == null) return;

            Db.Configure(ConfigParams.FromTuples(
                "connection.type", "mongodb",
                "connection.database", "test",
                "connection.uri", ""
            ));

            Db.OpenAsync(null).Wait();
            Db.ClearAsync(null).Wait();

            Fixture = new PersistenceFixture(Db, Db, Db, Db, Db, Db, Db, Db);
        }

        [Fact]
        public void TestCrudOperations()
        {
            Fixture?.TestCrudOperationsAsync().Wait();
        }

        [Fact]
        public void TestMultithreading()
        {
            Fixture?.TestMultithreading().Wait();
        }

        public void Dispose()
        {
            Db?.CloseAsync(null).Wait();
        }
    }
}
