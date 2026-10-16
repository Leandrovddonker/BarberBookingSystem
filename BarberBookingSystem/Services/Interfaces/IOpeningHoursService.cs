using BarberBookingSystem.Models;
using BarberBookingSystem.Models.Dtos;

namespace BarberBookingSystem.Services.Interfaces;

public interface IOpeningHoursService
{
    Task<List<OpeningHours>> GetAllAsync();
    Task<OpeningHoursDto> AddOpeningHoursAsync(OpeningHoursDto openingHours);
    Task<OpeningHoursDto> UpdateOpeningHoursByDayAsync(DayOfWeek day,OpeningHoursDto openingHours);
    Task<OpeningHours> DeleteOpeningHoursByIdAsync(int id);
}