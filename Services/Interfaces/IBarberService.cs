using planeProject.Models.Dtos;

namespace planeProject.Common;

public interface IBarberService
{
    Task<BarberDto> CreateBarberAsync(CreateBarberDto barberDto);
    Task<BarberDto> GetBarberByIdAsync(int id);
    Task<List<BarberDto>> GetAllBarbersAsync();
}