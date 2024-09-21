using MotoRent.Application.DTOs.Deliveryman;
using MotoRent.Application.DTOs.Motorcycle;

namespace MotoRent.Application.Services
{
    public interface IDeliverymanService
    {
        Task<DeliverymanDto> CreateDeliverymanAsync(CreateDeliverymanDto createDeliverymanDto);
        Task<DeliverymanDto> GetDeliverymanByIdAsync(string id);
        Task<string> GetRandomDeliverymanIdAsync();
        Task UpdateLicenseImageAsync(string id, UpdateLicenseImageDto updateLicenseImageDto);
    }
}