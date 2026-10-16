using planeProject.Models;
using planeProject.Models.Dtos;

namespace planeProject.Common;

public interface IOpeningHoursService
{
    Task<List<OpeningHours>> GetAllAsync();
    Task<OpeningHoursDto> AddOpeningHoursAsync(OpeningHoursDto openingHours);
    Task<OpeningHoursDto> UpdateOpeningHoursByDayAsync(DayOfWeek day,OpeningHoursDto openingHours);
    Task<OpeningHours> DeleteOpeningHoursByIdAsync(int id);
}