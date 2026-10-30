using BarberBookingSystem.Models.Dtos;

namespace BarberBookingSystem.Services.Interfaces;

public interface ISpecialOpeningHoursService
{
    Task<List<SpecialOpeningHoursResponseDto>> GetAllAsync();

    Task<SpecialOpeningHoursResponseDto?> GetByIdAsync(int id);

    Task<SpecialOpeningHoursResponseDto> CreateAsync(
        CreateSpecialOpeningHoursDto dto);

    Task<SpecialOpeningHoursResponseDto?> UpdateAsync(
        int id, UpdateSpecialOpeningHoursDto dto);

    Task<bool> DeleteAsync(int id);

    Task<int> GetReservationCountAsync(DateOnly date);
}