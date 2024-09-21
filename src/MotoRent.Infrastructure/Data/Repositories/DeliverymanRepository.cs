using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public class DeliverymanRepository : BaseRepository<DeliverymanModel>, IDeliverymanRepository
    {
        public DeliverymanRepository(IMongoDbContext context) : base(context, "deliverymen")
        {
        }

        public async Task<DeliverymanModel> GetByIdentifierAsync(string id)
        {
            var filter = Builders<DeliverymanModel>.Filter.Eq(d => d.Identifier, id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<DeliverymanModel> GetByCNPJAsync(string cnpj)
        {
            var filter = Builders<DeliverymanModel>.Filter.Eq(d => d.CNPJ, cnpj);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<DeliverymanModel> GetByLicenseNumberAsync(string licenseNumber)
        {
            var filter = Builders<DeliverymanModel>.Filter.Eq(d => d.LicenseNumber, licenseNumber);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task UpdateLicenseImageAsync(string id, string licenseImage)
        {
            var filter = Builders<DeliverymanModel>.Filter.Eq("identifier", id);
            var update = Builders<DeliverymanModel>.Update.Set(d => d.LicenseImage, licenseImage);
            await _collection.UpdateOneAsync(filter, update);
        }
    }
}