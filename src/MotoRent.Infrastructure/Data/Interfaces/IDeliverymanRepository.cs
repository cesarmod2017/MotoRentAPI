using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IDeliverymanRepository : IRepository<DeliverymanModel>
    {
        Task<DeliverymanModel> GetByCNPJAsync(string cnpj);
        Task<DeliverymanModel> GetByIdentifierAsync(string id);
        Task<DeliverymanModel> GetByLicenseNumberAsync(string licenseNumber);
        Task UpdateLicenseImageAsync(string id, string licenseImage);
    }
}
