﻿using System.Threading;
using PipServices.Data.Memory;
using PipServices.Commons.Config;
using Xunit;

namespace PipServices.Data.Test.Memory
{
    public sealed class IdentifiableMemoryPersistenceTest
    {
        private static IdentifiableMemoryPersistence<PersistenceFixture.Dummy, string> Db 
            = new IdentifiableMemoryPersistence<PersistenceFixture.Dummy, string>();
        private static PersistenceFixture Fixture { get; set; }

        private PersistenceFixture GetFixture()
        {
            return new PersistenceFixture(Db, Db, Db, Db, Db, Db, Db, Db);
        }

        public IdentifiableMemoryPersistenceTest()
        {
            if (Db == null)
                return;

            Db.Configure(new ConfigParams());

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
    }
}
