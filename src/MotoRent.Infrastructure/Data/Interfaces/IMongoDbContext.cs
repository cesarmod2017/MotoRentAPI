using MongoDB.Driver;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IMongoDbContext
    {
        IMongoCollection<T> GetCollection<T>(string name);
    }
}