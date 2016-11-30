using MongoDB.Driver;
using PipServices.Commons.Config;
using PipServices.Commons.Data;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using System;
using System.Threading.Tasks;
using PipServices.Commons.Reflect;

namespace PipServices.Data.MongoDb
{
    public class MongoDbPersistence<T, K> : IReferenceable, IConfigurable, IOpenable, IClosable, ICleanable,
        IWriter<T, K>, IGetter<T, K>, ISetter<T>
        where T : IIdentifiable<K>
        where K : class
    {
        private readonly string _collectionName;

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

        public MongoDbPersistence(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentNullException(nameof(collectionName));

            _collectionName = collectionName;
        }

        public void SetReferences(IReferences references)
        {
            // Todo: use composite logger
            var logger = (ILogger)references.GetOneOptional(new Descriptor("*", "logger", "*", "*", "*"));

            Logger = logger ?? Logger;
        }

        public void Configure(ConfigParams config)
        {
            // Todo: Use connection and auth components

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

        public Task OpenAsync(string correlationId)
        {
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

                Logger.Debug(correlationId, "Connected to mongodb database {0}, collection {1}", DatabaseName, _collectionName);

                return Task.Delay(0);
            }
            catch (Exception ex)
            {
                throw new ConnectionException(correlationId, "ConnectFailed", "Connection to mongodb failed", ex);
            }
        }

        public Task CloseAsync(string correlationId)
        {
            return Task.Delay(0);
        }

        public async Task<T> GetOneByIdAsync(string correlationId, K id)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Eq(x => x.Id, id);
            var result = await Collection.Find(filter).FirstOrDefaultAsync();

            Logger.Trace(correlationId, "Retrieved from {0} with id = {1}", _collectionName, id);

            return result;
        }

        public async Task<T> CreateAsync(string correlationId, T entity)
        {
            var identifiable = entity as IStringIdentifiable;
            if (identifiable != null && entity.Id == null)
                ObjectWriter.SetProperty(entity, nameof(entity.Id), IdGenerator.NextLong());

            await Collection.InsertOneAsync(entity, null);

            Logger.Trace(correlationId, "Created in {0} with id = {1}", _collectionName, entity.Id);

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
            var result = await Collection.FindOneAndReplaceAsync(filter, entity, options);

            Logger.Trace(correlationId, "Update in {0} with id = {1}", _collectionName, entity.Id);

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
            var result = await Collection.FindOneAndReplaceAsync(filter, entity, options);

            Logger.Trace(correlationId, "Set in {0} with id = {1}", _collectionName, entity.Id);

            return result;
        }

        public async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var options = new FindOneAndDeleteOptions<T>();
            var result =  await Collection.FindOneAndDeleteAsync(filter, options);

            Logger.Trace(correlationId, "Deleted from {0} with id = {1}", _collectionName, id);

            return result;
        }

        public Task ClearAsync(string correlationId)
        {
            return Database.DropCollectionAsync(_collectionName);
        }
    }
}