using MongoDB.Driver;
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
    public class MongoDbPersistence<T, K> : IReferenceable, IConfigurable, IOpenable, IClosable, ICleanable,
        IWriter<T, K>, IGetter<T, K>, ISetter<T>
        where T : IIdentifiable<K>
        where K : class
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

        public async Task<T> GetOneByIdAsync(string correlationId, K id)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Eq(x => x.Id, id);
            var result = await _collection.Find(filter).FirstOrDefaultAsync();

            _logger.Trace(correlationId, "Retrieved from {0} with id = {1}", _collectionName, id);

            return result;
        }

        public async Task<T> CreateAsync(string correlationId, T entity)
        {
            var identifiable = entity as IStringIdentifiable;
            if (identifiable != null && entity.Id == null)
                ObjectWriter.SetProperty(entity, nameof(entity.Id), IdGenerator.NextLong());

            await _collection.InsertOneAsync(entity, null);

            _logger.Trace(correlationId, "Created in {0} with id = {1}", _collectionName, entity.Id);

            return entity;
        }

        public async Task<T> UpdateAsync(string correlationId, T entity)
        {
            var identifiable = entity as IIdentifiable<K>;
            if (identifiable == null || entity.Id == null)
                return default(T);

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);
            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, entity, options);

            _logger.Trace(correlationId, "Update in {0} with id = {1}", _collectionName, entity.Id);

            return result;
        }

        public async Task<T> SetAsync(string correlationId, T entity)
        {
            var identifiable = entity as IIdentifiable<K>;
            if (identifiable == null || entity.Id == null)
                return default(T);

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);
            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, entity, options);

            _logger.Trace(correlationId, "Set in {0} with id = {1}", _collectionName, entity.Id);

            return result;
        }

        public async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var options = new FindOneAndDeleteOptions<T>();
            var result =  await _collection.FindOneAndDeleteAsync(filter, options);

            _logger.Trace(correlationId, "Deleted from {0} with id = {1}", _collectionName, id);

            return result;
        }

        public Task ClearAsync(string correlationId)
        {
            return _database.DropCollectionAsync(_collectionName);
        }
    }
}