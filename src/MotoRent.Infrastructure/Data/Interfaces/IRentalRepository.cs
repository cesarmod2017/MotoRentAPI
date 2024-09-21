using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IRentalRepository : IRepository<RentalModel>
    {
        Task<bool> ExistsForMotorcycleAsync(string motorcycleId);
        Task<RentalModel> GetByIdentifierAsync(string identifier);
        Task UpdateReturnDateAsync(string id, DateTime returnDate, decimal? totalCost);
    }
}
