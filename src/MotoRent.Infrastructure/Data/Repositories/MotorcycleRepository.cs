using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public class MotorcycleRepository : BaseRepository<MotorcycleModel>, IMotorcycleRepository
    {
        public MotorcycleRepository(IMongoDbContext context) : base(context, "motorcycles")
        {
        }
        public async Task<MotorcycleModel> GetByIdentifierAsync(string id)
        {
            var filter = Builders<MotorcycleModel>.Filter.Eq(d => d.Identifier, id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<MotorcycleModel>> GetByLicensePlateAsync(string licensePlate)
        {
            var filter = Builders<MotorcycleModel>.Filter.Eq(m => m.LicensePlate, licensePlate);
            return await _collection.Find(filter).ToListAsync();
        }
    }
}