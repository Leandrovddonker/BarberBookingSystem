using Microsoft.EntityFrameworkCore;
using planeProject.Data;
using planeProject.Exceptions;
using planeProject.Common;
using planeProject.Models;
using planeProject.Models.Dtos;

namespace planeProject.Services;

public class OpeningHourService(PlaneDbContext context) : IOpeningHoursService
{
    public async Task<OpeningHoursDto> AddOpeningHoursAsync(OpeningHoursDto openingHours)
    {
        if (!TimeSpan.TryParse(openingHours.OpeningTime, out var open))
        {
            throw new ArgumentException("OpeningTime must be in format HH:mm:ss");
        }

        if (!TimeSpan.TryParse(openingHours.ClosingTime, out var close))
        {
            throw new ArgumentException("ClosingTime must be in format HH:mm:ss");
        }
        
        var newOpeningHours = new OpeningHours()
        {
            Day = openingHours.Day,
            OpeningTime = TimeSpan.Parse(openingHours.OpeningTime),
            ClosingTime = TimeSpan.Parse(openingHours.ClosingTime)
        };
        context.Add(newOpeningHours);
        await context.SaveChangesAsync();
        return new OpeningHoursDto()
        {
            Day = newOpeningHours.Day,
            OpeningTime = newOpeningHours.OpeningTime.ToString(@"hh\:mm\:ss"),
            ClosingTime = newOpeningHours.ClosingTime.ToString(@"hh\:mm\:ss")
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
            oh.OpeningTime = TimeSpan.Parse(openingHours.OpeningTime);
            oh.ClosingTime = TimeSpan.Parse(openingHours.ClosingTime);
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