using BarberBookingSystem.Data;
using BarberBookingSystem.Models.Dtos;
using BarberBookingSystem.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BarberBookingSystem.Tests.Services;

public class OpeningHourServiceTests
{
    [Fact]
    public async Task CreateAndUpdate_RoundTripTimeOnlyValues()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new BarberSystemDbContext(new DbContextOptionsBuilder<BarberSystemDbContext>()
            .UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var service = new OpeningHourService(context);
        var dto = new OpeningHoursDto
        {
            Day = DayOfWeek.Monday,
            OpeningTime = new TimeOnly(9, 15, 30),
            ClosingTime = new TimeOnly(18, 45, 15)
        };
        var created = await service.AddOpeningHoursAsync(dto);
        Assert.Equal(dto.OpeningTime, created.OpeningTime);
        Assert.Equal(dto.ClosingTime, created.ClosingTime);
        dto.OpeningTime = new TimeOnly(10, 30);
        dto.ClosingTime = new TimeOnly(16, 0);
        await service.UpdateOpeningHoursByDayAsync(dto.Day, dto);
        context.ChangeTracker.Clear();
        var saved = Assert.Single(await service.GetAllAsync());
        Assert.Equal(dto.Day, saved.Day);
        Assert.Equal(dto.OpeningTime, saved.OpeningTime);
        Assert.Equal(dto.ClosingTime, saved.ClosingTime);
    }
}
