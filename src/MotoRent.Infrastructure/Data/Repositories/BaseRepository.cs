using MongoDB.Bson;
using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly IMongoCollection<T> _collection;

        protected BaseRepository(IMongoDbContext context, string collectionName)
        {
            _collection = context.GetCollection<T>(collectionName);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public virtual async Task<T> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
            {
                return null;
            }
            var filter = Builders<T>.Filter.Eq("_id", objectId);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<T> GetByFieldStringAsync(string fieldName, string value)
        {
            var filter = Builders<T>.Filter.Eq(fieldName, value);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            await _collection.InsertOneAsync(entity);
            return entity;
        }

        public virtual async Task UpdateAsync(string id, T entity)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
                return;

            var filter = Builders<T>.Filter.Eq("_id", objectId);
            await _collection.ReplaceOneAsync(filter, entity);
        }

        public virtual async Task UpdateByFieldAsync(string fieldName, string id, T entity)
        {


            var filter = Builders<T>.Filter.Eq(fieldName, id);
            await _collection.ReplaceOneAsync(filter, entity);
        }

        public virtual async Task DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
                return;
            var filter = Builders<T>.Filter.Eq("_id", objectId);
            await _collection.DeleteOneAsync(filter);
        }

        public virtual async Task DeleteAsync(string fieldName, string id)
        {
            var filter = Builders<T>.Filter.Eq(fieldName, id);
            await _collection.DeleteOneAsync(filter);
        }
    }
}