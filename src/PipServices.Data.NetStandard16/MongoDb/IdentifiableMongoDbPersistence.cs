using MongoDB.Driver;
using PipServices.Commons.Data;
using PipServices.Commons.Reflect;
using System.Threading.Tasks;

namespace PipServices.Data.MongoDb
{
    public class IdentifiableMongoDbPersistence<T, K> : MongoDbPersistence<T>, IWriter<T, K>, IGetter<T, K>, ISetter<T>
        where T : IIdentifiable<K>
        where K : class
    {
        public IdentifiableMongoDbPersistence(string collectionName)
            : base(collectionName)
        { }

        public async Task<T> GetOneByIdAsync(string correlationId, K id)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Eq(x => x.Id, id);
            var result = await _collection.Find(filter).FirstOrDefaultAsync();

            _logger.Trace(correlationId, "Retrieved from {0} with id = {1}", _collectionName, id);

            return result;
        }

        public async Task<T> CreateAsync(string correlationId, T item)
        {
            var identifiable = item as IStringIdentifiable;
            if (identifiable != null && item.Id == null)
                ObjectWriter.SetProperty(item, nameof(item.Id), IdGenerator.NextLong());

            await _collection.InsertOneAsync(item, null);

            _logger.Trace(correlationId, "Created in {0} with id = {1}", _collectionName, item.Id);

            return item;
        }

        public async Task<T> UpdateAsync(string correlationId, T item)
        {
            var identifiable = item as IIdentifiable<K>;
            if (identifiable == null || item.Id == null)
                return default(T);

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);
            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, item, options);

            _logger.Trace(correlationId, "Update in {0} with id = {1}", _collectionName, item.Id);

            return result;
        }

        public async Task<T> SetAsync(string correlationId, T item)
        {
            var identifiable = item as IIdentifiable<K>;
            if (identifiable == null || item.Id == null)
                return default(T);

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);
            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, item, options);

            _logger.Trace(correlationId, "Set in {0} with id = {1}", _collectionName, item.Id);

            return result;
        }

        public async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var options = new FindOneAndDeleteOptions<T>();
            var result = await _collection.FindOneAndDeleteAsync(filter, options);

            _logger.Trace(correlationId, "Deleted from {0} with id = {1}", _collectionName, id);

            return result;
        }
    }
}