﻿using MongoDB.Driver;
using PipServices.Commons.Auth;
using PipServices.Commons.Config;
using PipServices.Commons.Connect;
using PipServices.Commons.Data;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Reflect;
using PipServices.Commons.Run;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices.Data.MongoDb
{
    public class MongoDbPersistence<T> : IReferenceable, IReconfigurable, IOpenable, ICleanable
    {
        private ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "connection.type", "mongodb",
            "connection.database", "test",
            "connection.host", "localhost",
            "connection.port", 27017,

            "options.poll_size", 4,
            "options.keep_alive", 1,
            "options.connect_timeout", 5000,
            "options.auto_reconnect", true,
            "options.max_page_size", 100,
            "options.debug", true
        );

        protected readonly string _collectionName;
        protected ConnectionResolver _connectionResolver = new ConnectionResolver();
        protected CredentialResolver _credentialResolver = new CredentialResolver();
        protected ConfigParams _options = new ConfigParams();

        protected MongoClient _connection;
        protected IMongoDatabase _database;
        protected IMongoCollection<T> _collection;

        protected CompositeLogger _logger = new CompositeLogger();

        public MongoDbPersistence(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentNullException(nameof(collectionName));

            _collectionName = collectionName;
        }

        public void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _connectionResolver.SetReferences(references);
            _credentialResolver.SetReferences(references);
        }

        public void Configure(ConfigParams config)
        {
            config = config.SetDefaults(_defaultConfig);

            _connectionResolver.Configure(config, true);
            _credentialResolver.Configure(config, true);

            _options = _options.Override(config.GetSection("options"));
        }

        public bool IsOpened()
        {
            return _collection != null;
        }

        public async virtual Task OpenAsync(string correlationId)
        {
            var connection = await _connectionResolver.ResolveAsync(correlationId);
            var credential = await _credentialResolver.LookupAsync(correlationId);
            await OpenAsync(correlationId, connection, credential);
        }

        public async Task OpenAsync(string correlationId, ConnectionParams connection, CredentialParams credential)
        {
            if (connection == null)
                throw new ConfigException(correlationId, "NO_CONNECTION", "Database connection is not set");

            var host = connection.Host;
            if (host == null)
                throw new ConfigException(correlationId, "NO_HOST", "Connection host is not set");

            var port = connection.Port;
            if (port == 0)
                throw new ConfigException(correlationId, "NO_PORT", "Connection port is not set");

            var databaseName = connection.GetAsNullableString("database");
            if (databaseName == null)
                throw new ConfigException(correlationId, "NO_DATABASE", "Connection database is not set");

            _logger.Trace(correlationId, "Connecting to mongodb database {0}, collection {1}", databaseName, _collectionName);

            try
            {
                var settings = new MongoClientSettings
                {
                    Server = new MongoServerAddress(host, port),
                    MaxConnectionPoolSize =  _options.GetAsInteger("poll_size"),
                    ConnectTimeout = _options.GetAsTimeSpan("connect_timeout"),
                    //SocketTimeout =
                    //    new TimeSpan(options.GetInteger("server.socketOptions.socketTimeoutMS")*
                    //                 TimeSpan.TicksPerMillisecond)
                };

                if (credential.Username != null)
                {
                    var dbCredential = MongoCredential.CreateCredential(databaseName, credential.Username, credential.Password);
                    settings.Credentials = new[] { dbCredential };
                }

                _connection = new MongoClient(settings);
                _database = _connection.GetDatabase(databaseName);
                _collection = _database.GetCollection<T>(_collectionName);

                _logger.Debug(correlationId, "Connected to mongodb database {0}, collection {1}", databaseName, _collectionName);
            }
            catch (Exception ex)
            {
                throw new ConnectionException(correlationId, "ConnectFailed", "Connection to mongodb failed", ex);
            }

            await Task.Delay(0);
        }

        public Task CloseAsync(string correlationId)
        {
            return Task.Delay(0);
        }

        public Task ClearAsync(string correlationId)
        {
            return _database.DropCollectionAsync(_collectionName);
        }
    }
}