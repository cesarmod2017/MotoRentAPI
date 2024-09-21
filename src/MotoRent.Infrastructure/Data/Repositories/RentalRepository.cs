using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public class RentalRepository : BaseRepository<RentalModel>, IRentalRepository
    {
        public RentalRepository(IMongoDbContext context) : base(context, "rentals")
        {
        }

        public async Task<RentalModel> GetByIdentifierAsync(string identifier)
        {
            var filter = Builders<RentalModel>.Filter.Eq(r => r.Identifier, identifier);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        // Método existente
        public async Task UpdateReturnDateAsync(string id, DateTime returnDate, decimal? totalCost)
        {
            var filter = Builders<RentalModel>.Filter.Eq(r => r.Identifier, id);
            var update = Builders<RentalModel>.Update
                .Set(r => r.ReturnDate, returnDate)
                .Set(r => r.TotalCost, totalCost);
            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task<bool> ExistsForMotorcycleAsync(string motorcycleId)
        {
            var filter = Builders<RentalModel>.Filter.Eq(r => r.MotorcycleId, motorcycleId);
            return await _collection.CountDocumentsAsync(filter) > 0;
        }
    }
}
