using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IMotorcycleRepository : IRepository<MotorcycleModel>
    {
        Task<MotorcycleModel> GetByIdentifierAsync(string id);
        Task<IEnumerable<MotorcycleModel>> GetByLicensePlateAsync(string licensePlate);
    }
}
