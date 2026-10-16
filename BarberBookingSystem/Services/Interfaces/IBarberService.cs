using BarberBookingSystem.Models.Dtos;

namespace BarberBookingSystem.Common;

public interface IBarberService
{
    Task<BarberDto> CreateBarberAsync(CreateBarberDto barberDto);
    Task<BarberDto> GetBarberByIdAsync(int id);
    Task<List<BarberDto>> GetAllBarbersAsync();
}