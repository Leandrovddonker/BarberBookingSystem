using BarberBookingSystem.Data;
using BarberBookingSystem.Exceptions;
using BarberBookingSystem.Models;
using BarberBookingSystem.Models.Dtos;
using BarberBookingSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BarberBookingSystem.Services;

public class SpecialOpeningHoursService(BarberSystemDbContext context) : ISpecialOpeningHoursService
{
    public async Task<List<SpecialOpeningHoursResponseDto>> GetAllAsync()
    {
        var hours = await context.SpecialOpeningHours.AsNoTracking().OrderBy(s => s.Date).ToListAsync();
        return hours.Select(Map).ToList();
    }

    public async Task<SpecialOpeningHoursResponseDto?> GetByIdAsync(int id)
    {
        var hours = await context.SpecialOpeningHours.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
        return hours == null ? throw new NotFoundException("Special opening hours not found") : Map(hours);
    }

    public async Task<SpecialOpeningHoursResponseDto> CreateAsync(
        CreateSpecialOpeningHoursDto dto)
    {
        var exists = await context.SpecialOpeningHours
            .AnyAsync(h => h.Date == dto.Date);

        if (exists)
            throw new ConflictException("Special opening hours already exist for this date.");

        Validate(dto.IsClosed, dto.OpeningTime, dto.ClosingTime);

        var hours = new SpecialOpeningHours
        {
            Date = dto.Date,
            OpeningTime = dto.OpeningTime,
            ClosingTime = dto.ClosingTime,
            IsClosed = dto.IsClosed
        };

        context.SpecialOpeningHours.Add(hours);
        await context.SaveChangesAsync();

        return Map(hours);
    }

    public async Task<SpecialOpeningHoursResponseDto?> UpdateAsync(int id, UpdateSpecialOpeningHoursDto dto)
    {
        var hours = await context.SpecialOpeningHours.SingleOrDefaultAsync(s => s.Id == id);
        if (hours == null) throw new NotFoundException("Special opening hours not found.");
        Validate(dto.IsClosed, dto.OpeningTime, dto.ClosingTime);
        hours.IsClosed = dto.IsClosed;
        hours.OpeningTime = dto.OpeningTime;
        hours.ClosingTime = dto.ClosingTime;
        await context.SaveChangesAsync();
        return Map(hours);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var hours = await context.SpecialOpeningHours.SingleOrDefaultAsync(s => s.Id == id);
        if (hours == null) throw new NotFoundException("Special opening hours not found.");
        context.SpecialOpeningHours.Remove(hours);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetReservationCountAsync(DateOnly date)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), timeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(date.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
        return await context.Reservations.CountAsync(r => r.StartTime >= startUtc && r.StartTime < endUtc);
    }

    private static void Validate(bool isClosed, TimeOnly? openingTime, TimeOnly? closingTime)
    {
        if (isClosed)
        {
            if (openingTime.HasValue || closingTime.HasValue)
                throw new BadRequestException("OpeningTime and ClosingTime must be null for a closed date.");
            return;
        }
        if (!openingTime.HasValue || !closingTime.HasValue)
            throw new BadRequestException("OpeningTime and ClosingTime are required for an open date.");
        if (openingTime.Value >= closingTime.Value)
            throw new BadRequestException("OpeningTime must be earlier than ClosingTime.");
    }

    private static SpecialOpeningHoursResponseDto Map(SpecialOpeningHours hours) => new()
    {
        Id = hours.Id,
        Date = hours.Date,
        IsClosed = hours.IsClosed,
        OpeningTime = hours.OpeningTime,
        ClosingTime = hours.ClosingTime
    };
}
