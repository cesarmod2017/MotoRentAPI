using MotoRent.Application.DTOs.Rental;

namespace MotoRent.Application.Services
{
    public interface IRentalService
    {
        Task<RentalDto> CreateRentalAsync(CreateRentalDto createRentalDto);
        Task<RentalDto> GetRentalByIdAsync(string id);
        Task<RentalCalculationResultDto> CalculateRentalCostAsync(string id, UpdateReturnDateDto returnDateDto);
    }
}