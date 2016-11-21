﻿using System.Threading;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Data.MongoDb;
using Xunit;

namespace PipServices.Data.Test.MongoDb
{
    public sealed class MongoDbPersistenceTest
    {
        private static MongoDbPersistence<PersistenceFixture.Dummy, string> Db { get; } = new MongoDbPersistence<PersistenceFixture.Dummy, string>("dummies", new Descriptor("pip-services-data", "prsistance", "mongodb", "1.0"));
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
