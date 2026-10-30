using BarberBookingSystem.Data;
using BarberBookingSystem.Exceptions;
using BarberBookingSystem.Models;
using BarberBookingSystem.Models.Dtos;
using BarberBookingSystem.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BarberBookingSystem.Tests.Services;

public class SpecialOpeningHoursServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly BarberSystemDbContext context;
    private readonly SpecialOpeningHoursService service;
    private static readonly DateOnly Date = new(2026, 12, 25);

    public SpecialOpeningHoursServiceTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        context = new BarberSystemDbContext(new DbContextOptionsBuilder<BarberSystemDbContext>()
            .UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        service = new SpecialOpeningHoursService(context);
    }

    [Fact]
    public async Task Crud_PersistsAndMapsHoursInDateOrder()
    {
        var later = await service.CreateAsync(new CreateSpecialOpeningHoursDto
            { Date = Date.AddDays(1), IsClosed = true });
        var created = await service.CreateAsync(new CreateSpecialOpeningHoursDto
        {
            Date = Date, OpeningTime = new TimeOnly(9, 0), ClosingTime = new TimeOnly(17, 0)
        });
        Assert.True(created.Id > 0);
        var found = await service.GetByIdAsync(created.Id);
        Assert.NotNull(found);
        Assert.Equal(Date, found.Date);
        Assert.False(found.IsClosed);
        Assert.Equal(new TimeOnly(9, 0), found.OpeningTime);
        Assert.Equal(new TimeOnly(17, 0), found.ClosingTime);
        Assert.Equal(new[] { created.Id, later.Id }, (await service.GetAllAsync()).Select(s => s.Id));

        var updated = await service.UpdateAsync(created.Id, new UpdateSpecialOpeningHoursDto { IsClosed = true });
        Assert.NotNull(updated);
        Assert.Equal(Date, updated.Date);
        Assert.True(updated.IsClosed);
        Assert.Null(updated.OpeningTime);
        Assert.Null(updated.ClosingTime);
        updated = await service.UpdateAsync(created.Id, new UpdateSpecialOpeningHoursDto
            { OpeningTime = new TimeOnly(10, 0), ClosingTime = new TimeOnly(16, 0) });
        Assert.False(updated!.IsClosed);
        context.ChangeTracker.Clear();
        Assert.Equal(new TimeOnly(10, 0), (await service.GetByIdAsync(created.Id))!.OpeningTime);
        Assert.True(await service.DeleteAsync(created.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(created.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(created.Id,
            new UpdateSpecialOpeningHoursDto { IsClosed = true }));
        Assert.Single(await service.GetAllAsync());
    }

    [Fact]
    public async Task DuplicateDate_IsRejectedByServiceAndDatabase()
    {
        await service.CreateAsync(new CreateSpecialOpeningHoursDto { Date = Date, IsClosed = true });
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            new CreateSpecialOpeningHoursDto { Date = Date, IsClosed = true }));
        context.SpecialOpeningHours.Add(new SpecialOpeningHours { Date = Date, IsClosed = true });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(true, 9, null)]
    [InlineData(true, null, 17)]
    [InlineData(true, 9, 17)]
    [InlineData(false, null, null)]
    [InlineData(false, 9, null)]
    [InlineData(false, null, 17)]
    [InlineData(false, 17, 9)]
    [InlineData(false, 9, 9)]
    public async Task InvalidHours_AreRejectedOnCreateAndUpdate(bool closed, int? open, int? close)
    {
        TimeOnly? opening = open.HasValue ? new TimeOnly(open.Value, 0) : null;
        TimeOnly? closing = close.HasValue ? new TimeOnly(close.Value, 0) : null;
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateSpecialOpeningHoursDto
            { Date = Date, IsClosed = closed, OpeningTime = opening, ClosingTime = closing }));
        Assert.Empty(await service.GetAllAsync());
        var existing = await service.CreateAsync(new CreateSpecialOpeningHoursDto { Date = Date, IsClosed = true });
        await Assert.ThrowsAsync<BadRequestException>(() => service.UpdateAsync(existing.Id,
            new UpdateSpecialOpeningHoursDto { IsClosed = closed, OpeningTime = opening, ClosingTime = closing }));
        var saved = await context.SpecialOpeningHours.AsNoTracking().SingleAsync();
        Assert.True(saved.IsClosed);
        Assert.Null(saved.OpeningTime);
        Assert.Null(saved.ClosingTime);
    }

    [Theory]
    [InlineData(2026, 12, 25, 24)]
    [InlineData(2026, 7, 1, 24)]
    [InlineData(2026, 3, 29, 23)]
    [InlineData(2026, 10, 25, 25)]
    public async Task ReservationCount_UsesAmsterdamBoundariesAndCrudPreservesReservations(
        int year, int month, int day, int hours)
    {
        var date = new DateOnly(year, month, day);
        Assert.Equal(0, await service.GetReservationCountAsync(date));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var start = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), zone);
        var end = TimeZoneInfo.ConvertTimeToUtc(date.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
        Assert.Equal(hours, (end - start).TotalHours);
        var user = new User { Name = "Customer", Email = "customer@test.com", Password = "test", PhoneNumber = "123" };
        var barber = new Barber
        {
            Name = "Barber",
            User = new User { Name = "Barber", Email = "barber@test.com", Password = "test", PhoneNumber = "456" }
        };
        var cut = new HairCut { Cut = "Cut", Price = 25m, DurationMinutes = 30 };
        var times = new[] { start.AddTicks(-1), start, end.AddTicks(-1), end };
        foreach (var time in times)
            context.Reservations.Add(new Reservation { User = user, Barber = barber, HairCut = cut, StartTime = time });
        await context.SaveChangesAsync();
        Assert.Equal(2, await service.GetReservationCountAsync(date));
        var created = await service.CreateAsync(new CreateSpecialOpeningHoursDto { Date = date, IsClosed = true });
        await service.UpdateAsync(created.Id, new UpdateSpecialOpeningHoursDto
            { OpeningTime = new TimeOnly(10, 0), ClosingTime = new TimeOnly(12, 0) });
        await service.DeleteAsync(created.Id);
        Assert.Equal(2, await service.GetReservationCountAsync(date));
        Assert.Equal(times, await context.Reservations.AsNoTracking().OrderBy(r => r.StartTime).Select(r => r.StartTime).ToArrayAsync());
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }
}
