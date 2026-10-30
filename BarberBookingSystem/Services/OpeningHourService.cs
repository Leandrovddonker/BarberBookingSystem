using BarberBookingSystem.Services.Interfaces;
using BarberBookingSystem.Data;
using BarberBookingSystem.Exceptions;
using BarberBookingSystem.Models;
using BarberBookingSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BarberBookingSystem.Services;

public class OpeningHourService(BarberSystemDbContext context) : IOpeningHoursService
{
    public async Task<OpeningHoursDto> AddOpeningHoursAsync(OpeningHoursDto openingHours)
    {
        var newOpeningHours = new OpeningHours()
        {
            Day = openingHours.Day,
            OpeningTime = openingHours.OpeningTime,
            ClosingTime = openingHours.ClosingTime
        };
        context.Add(newOpeningHours);
        await context.SaveChangesAsync();
        return new OpeningHoursDto()
        {
            Day = newOpeningHours.Day,
            OpeningTime = newOpeningHours.OpeningTime,
            ClosingTime = newOpeningHours.ClosingTime
        };
    }

    public async Task<List<OpeningHours>> GetAllAsync()
    {
        var all = await context.OpeningHours.ToListAsync();
        return all;
    }

    public async Task<OpeningHoursDto> UpdateOpeningHoursByDayAsync(DayOfWeek day,OpeningHoursDto openingHours)
    {
        var oh = await context.OpeningHours.FirstOrDefaultAsync(o => o.Day == day);
        if (oh != null)
        {
            oh.Day = openingHours.Day;
            oh.OpeningTime = openingHours.OpeningTime;
            oh.ClosingTime = openingHours.ClosingTime;
        }
        await context.SaveChangesAsync();
        return openingHours;
    }

    public async Task<OpeningHours> DeleteOpeningHoursByIdAsync(int id)
    {
        var openingHours = await context.OpeningHours.FirstOrDefaultAsync(o => o.Id == id);
        if (openingHours == null)
        {
            throw new NotFoundException("Opening hour not found");
        }
        context.OpeningHours.Remove(openingHours);
        await context.SaveChangesAsync();
        return openingHours;
    }
}