using MotoRent.Application.DTOs.Motorcycle;

namespace MotoRent.Application.Services
{
    public interface IMotorcycleService
    {
        Task<IEnumerable<MotorcycleDto>> GetAllMotorcyclesAsync();
        Task<MotorcycleDto> GetMotorcycleByIdAsync(string id);
        Task<MotorcycleDto> CreateMotorcycleAsync(CreateMotorcycleDto createMotorcycleDto);
        Task UpdateMotorcycleLicensePlateAsync(string id, UpdateLicensePlateDto updateLicensePlateDto);
        Task DeleteMotorcycleAsync(string id);
        Task<IEnumerable<MotorcycleDto>> GetMotorcyclesByLicensePlateAsync(string licensePlate);
        Task<string> GetRandomMotorcycleIdAsync();
    }
}