using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using PipServices.Commons.Config;
using PipServices.Commons.Data;
using PipServices.Commons.Errors;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Commons.Log;
using PipServices.Data.Interfaces;

namespace PipServices.Data.MongoDb
{
    public class MongoDbPersistence<T, TI> : IReferenceable, IConfigurable, IOpenable, IClosable, ICleanable,
        IWriter<T, TI>, IGetter<T, TI>, ISetter<T>, IDescriptable
        where T : IIdentifiable<TI>
        where TI : class
    {
        private readonly string _collectionName;
        private readonly Descriptor _descriptor;

        private const string DefaultHost = "localhost";
        private const int DefaultPort = 27017;
        private const int DefaultPollSize = 4;
        private const int DefaultKeepAlive = 1;
        private const int DefaultConnectTimeoutMs = 5000;
        private const bool DefaultAutoReconnect = true;
        private const int DefaultMaxPageSize = 100;
        private const bool DefaultDebug = true;

        protected string Host = DefaultHost;
        protected int Port = DefaultPort;
        protected string DatabaseName;

        protected int PollSize = DefaultPollSize;
        protected int KeepAlive = DefaultKeepAlive;
        protected int ConnectTimeoutMs = DefaultConnectTimeoutMs;
        protected bool AutoReconnect = DefaultAutoReconnect;
        protected int MaxPageSize = DefaultMaxPageSize;
        protected bool Debug = DefaultDebug;

        protected MongoClient Connection { get; private set; }
        public IMongoDatabase Database { get; private set; }
        public IMongoCollection<T> Collection { get; private set; }

        protected ILogger Logger = new NullLogger();

        public MongoDbPersistence(string collectionName, Descriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentNullException(nameof(collectionName));
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            _collectionName = collectionName;
            _descriptor = descriptor;
        }

        public void SetReferences(IReferences references)
        {
            var logger = (ILogger)references.GetOneOptional(new Descriptor("*", "logger", "*", "*"));

            Logger = logger ?? Logger;
        }

        public void Configure(ConfigParams config)
        {
            var connectionType = config.GetAsNullableString("connection.type");
            DatabaseName = config.GetAsNullableString("connection.database");
            //Uri = config.GetAsNullableString("connection.uri");

            Host = config.GetAsStringWithDefault("connection.host", DefaultHost);
            Port = config.GetAsIntegerWithDefault("connection.port", DefaultPort);

            PollSize = config.GetAsIntegerWithDefault("options.server.pollSize", DefaultPollSize);
            KeepAlive = config.GetAsIntegerWithDefault("options.server.socketOptions.keepAlive", DefaultKeepAlive);
            ConnectTimeoutMs = config.GetAsIntegerWithDefault("options.server.socketOptions.connectTimeoutMS", DefaultConnectTimeoutMs);
            AutoReconnect = config.GetAsBooleanWithDefault("options.server.auto_reconnect", DefaultAutoReconnect);
            MaxPageSize = config.GetAsIntegerWithDefault("options.max_page_size", DefaultMaxPageSize);
            Debug = config.GetAsBooleanWithDefault("options.debug", DefaultDebug);

            if (string.IsNullOrWhiteSpace(connectionType) || connectionType != "mongodb")
                throw new ConfigException(null, "WrongConnectionType", "MongoDb is the only supported connection type");

            if (string.IsNullOrWhiteSpace(DatabaseName))
                throw new ConfigException(null, "NoConnectionDatabase", "Connection database is not set");

            //if (string.IsNullOrWhiteSpace(Uri))
            //    throw new ConfigException(null, "NoConnectionUri", "Connection Uri is not set");

            //if (connection == null)
            //    throw new ConfigError(this, "NoConnection", "Database connection is not set");

            //if (connection.Host == null)
            //    throw new ConfigError(this, "NoConnectionHost", "Connection host is not set");

            //if (connection.Port == 0)
            //    throw new ConfigError(this, "NoConnectionPort", "Connection port is not set");
        }

        public Task OpenAsync(string correlationId, CancellationToken token)
        {
            Logger.Trace(correlationId, "Component " + _descriptor + " opening");

            Logger.Trace(correlationId, "Connecting to mongodb database {0}, collection {1}", DatabaseName, _collectionName);

            try
            {
                var settings = new MongoClientSettings
                {
                    Server = new MongoServerAddress(Host, Port),
                    MaxConnectionPoolSize = PollSize,
                    ConnectTimeout = TimeSpan.FromMilliseconds(ConnectTimeoutMs),
                    //SocketTimeout =
                    //    new TimeSpan(options.GetInteger("server.socketOptions.socketTimeoutMS")*
                    //                 TimeSpan.TicksPerMillisecond)
                };

                Connection = new MongoClient(settings);
                Database = Connection.GetDatabase(DatabaseName);
                Collection = Database.GetCollection<T>(_collectionName);

                Logger.Trace(correlationId, "Component " + _descriptor + " opened"); 

                return Task.CompletedTask;
            }
            catch (Exception)
            {
                throw new ConnectionException(correlationId, "ConnectFailed", "Connection to mongodb failed");
            }
        }

        public Task CloseAsync(string correlationId, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public Task ClearAsync(string correlationId, CancellationToken token)
        {
            return Database.DropCollectionAsync(_collectionName, token);
        }

        public async Task<T> CreateAsync(string correlationId, T entity, CancellationToken token)
        {
            var identifiable = entity as IStringIdentifiable;
            if (identifiable != null && entity.Id == null)
                identifiable.Id = IdGenerator.NextLong();

            await Collection.InsertOneAsync(entity, null, token);

            Logger.Trace(correlationId, "Created in {0} with id = {1}", _collectionName, entity.Id);

            return entity;
        }

        public Task<T> UpdateAsync(string correlationId, T entity, CancellationToken token)
        {
            var identifiable = entity as IIdentifiable<TI>;
            if (identifiable == null || entity.Id == null)
                return Task.FromResult(default(T));

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);

            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };

            var task = Collection.FindOneAndReplaceAsync(filter, entity, options, token);

            Logger.Trace(correlationId, "Update in {0} with id = {1}", _collectionName, entity.Id);

            return task;
        }

        public Task<T> DeleteByIdAsync(string correlationId, TI id, CancellationToken token)
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);

            var options = new FindOneAndDeleteOptions<T>();

            var task =  Collection.FindOneAndDeleteAsync(filter, options, token);

            Logger.Trace(correlationId, "Deleted from {0} with id = {1}", _collectionName, id);

            return task;
        }

        public Task<T> GetOneByIdAsync(string correlationId, TI id, CancellationToken token)
        {
            var builder = Builders<T>.Filter;

            var filter = builder.Eq(x => x.Id, id);

            var task = Collection.Find(filter).FirstOrDefaultAsync(token);

            Logger.Trace(correlationId, "Retrieved from {0} with id = {1}", _collectionName, id);

            return task;
        }

        public Task<T> SetAsync(string correlationId, T entity, CancellationToken token)
        {
            var identifiable = entity as IIdentifiable<TI>;
            if (identifiable == null || entity.Id == null)
                return Task.FromResult(default(T));

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);

            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };

            var task = Collection.FindOneAndReplaceAsync(filter, entity, options, token);

            Logger.Trace(correlationId, "Set in {0} with id = {1}", _collectionName, entity.Id);

            return task;
        }

        public Descriptor GetDescriptor()
        {
            return _descriptor;
        }
    }
}