using BarberBookingSystem.Data;
using BarberBookingSystem.Exceptions;
using BarberBookingSystem.Models;
using BarberBookingSystem.Models.Dtos;
using BarberBookingSystem.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BarberBookingSystem.Tests.Services;

public class ReservationServiceTests
{
    private static (BarberSystemDbContext Context, SqliteConnection Connection)
        CreateTestDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BarberSystemDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new BarberSystemDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
    
    private static async Task<(Barber Barber, HairCut Haircut, User Customer)>
        SeedTestDataAsync(BarberSystemDbContext context, bool addOpeningHours = true)
    {
        var barber = new Barber
        {
            Name = "Test Barber",
            User = new User
            {
                Name = "Test Barber User",
                Email = "barber@example.com",
                Password = "test",
                PhoneNumber = "0612345678"
            }
        };

        var customer = new User
        {
            Name = "Test Customer",
            Email = "customer@example.com",
            Password = "test",
            PhoneNumber = "0687654321"
        };

        var haircut = new HairCut
        {
            Cut = "Standard Haircut",
            Price = 25m,
            DurationMinutes = 30
        };

        context.Barbers.Add(barber);
        context.Users.Add(customer);
        context.HairCuts.Add(haircut);

        if (addOpeningHours)
        {
            context.OpeningHours.Add(new OpeningHours
            {
                Day = DayOfWeek.Monday,
                OpeningTime = new TimeSpan(9, 0, 0),
                ClosingTime = new TimeSpan(10, 0, 0)
            });
        }

        await context.SaveChangesAsync();

        return (barber, haircut, customer);
    }
    private static async Task<User> CreateSecondCustomerAsync(
        BarberSystemDbContext context)
    {
        var secondCustomer = new User
        {
            Name = "Second Customer",
            Email = "second@example.com",
            Password = "test",
            PhoneNumber = "0611111111"
        };

        context.Users.Add(secondCustomer);
        await context.SaveChangesAsync();

        return secondCustomer;
    }
    
    private static async Task AddReservationAsync(
        BarberSystemDbContext context,
        Barber barber,
        HairCut haircut,
        User customer,
        DateTime startTime)
    {
        var reservation = new Reservation
        {
            Barber = barber,
            HairCut = haircut,
            User = customer,
            StartTime = startTime
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();
    }
    
    [Fact]
    public void CreateTestDatabase_ShouldCreateDatabase()
    {
        var (context, connection) = CreateTestDatabase();

        using (context)
        using (connection)
        {
            Assert.True(context.Database.CanConnect());
        }
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenSalonIsClosed_ReturnsEmptyList()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            {
                // Arrange
                var (barber, haircut, _) =
                    await SeedTestDataAsync(context, addOpeningHours: false);

                var service = new ReservationService(context);
                var date = new DateOnly(2026, 10, 19);

                // Act
                var result = await service.GetAvailableSlotsAsync(
                    barber.Id,
                    haircut.Id,
                    date
                );

                // Assert
                Assert.Empty(result);
            }
        }
    }
    [Fact]
    public async Task GetAvailableSlotsAsync_WhenSalonIsOpen_ReturnsAvailableSlots()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var service = new ReservationService(context);

            // Future Monday
            var date = new DateOnly(2027, 10, 18);

            // Act
            var result = await service.GetAvailableSlotsAsync(
                barber.Id,
                haircut.Id,
                date
            );

            // Assert
            Assert.Equal(3, result.Count);
            
            Assert.Equal(new DateTime(2027, 10, 18, 7, 0, 0, DateTimeKind.Utc), result[0]);
            Assert.Equal(new DateTime(2027, 10, 18, 7, 15, 0, DateTimeKind.Utc), result[1]);
            Assert.Equal(new DateTime(2027, 10, 18, 7, 30, 0, DateTimeKind.Utc), result[2]);
        }
    }
    
    [Fact]
    public async Task GetAvailableSlotsAsync_WhenReservationExists_ExcludesOverlappingSlots()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                new DateTime(2027, 10, 18, 7, 15, 0, DateTimeKind.Utc)
            );

            var service = new ReservationService(context);

            // Act
            var result = await service.GetAvailableSlotsAsync(
                barber.Id,
                haircut.Id,
                new DateOnly(2027, 10, 18)
            );

            // Assert
            Assert.Empty(result);
        }
    }
    
    [Fact]
    public async Task GetAvailableSlotsAsync_WhenReservationEnds_AllowsNextSlot()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                new DateTime(2027, 10, 18, 7, 0, 0, DateTimeKind.Utc)
            );

            var service = new ReservationService(context);

            // Act
            var result = await service.GetAvailableSlotsAsync(
                barber.Id,
                haircut.Id,
                new DateOnly(2027, 10, 18)
            );

            // Assert
            Assert.Single(result);

            Assert.Equal(
                new DateTime(2027, 10, 18, 7, 30, 0, DateTimeKind.Utc),
                result[0]
            );
        }
    }
    [Fact]
    public async Task CreateReservation_WhenValid_SavesReservationToDatabase()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var service = new ReservationService(context);

            // 18 October 2027, 09:00 Dutch time = 07:00 UTC
            var startTime = new DateTime(
                2027, 10, 18, 7, 0, 0, DateTimeKind.Utc);

            var reservationDto = new CreateReservationDto
            {
                BarberId = barber.Id,
                HairCutId = haircut.Id,
                StartTime = startTime
            };

            // Act
            var result = await service.CreateReservation(
                reservationDto,
                customer.Id
            );

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal(barber.Id, result.BarberId);
            Assert.Equal(haircut.Id, result.HairCutId);
            Assert.Equal(customer.Id, result.UserId);
            Assert.Equal(startTime, result.StartTime);

            var savedReservation = await context.Reservations
                .SingleAsync(r => r.Id == result.Id);

            Assert.Equal(customer.Id, savedReservation.UserId);
            Assert.Equal(barber.Id, savedReservation.BarberId);
            Assert.Equal(haircut.Id, savedReservation.HaircutId);
            Assert.Equal(startTime, savedReservation.StartTime);
        }
    }
    [Fact]
    public async Task CreateReservation_WhenTimeSlotOverlapsExistingReservation_ThrowsConflictException()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            // Existing reservation at 09:00
            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                new DateTime(2027, 10, 18, 7, 0, 0, DateTimeKind.Utc)
            );

            var service = new ReservationService(context);

            // New reservation at 09:15 (overlap)
            var reservationDto = new CreateReservationDto
            {
                BarberId = barber.Id,
                HairCutId = haircut.Id,
                StartTime = new DateTime(
                    2027, 10, 18, 7, 15, 0, DateTimeKind.Utc)
            };

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(
                () => service.CreateReservation(reservationDto, customer.Id)
            );

            Assert.Equal(1, await context.Reservations.CountAsync());
        }
    }
    [Fact]
    public async Task CreateReservation_WhenOutsideOpeningHours_ThrowsBadRequestException()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var service = new ReservationService(context);

            var reservationDto = new CreateReservationDto
            {
                BarberId = barber.Id,
                HairCutId = haircut.Id,
                // 09:45 Dutch time = 07:45 UTC
                StartTime = new DateTime(
                    2027, 10, 18, 7, 45, 0, DateTimeKind.Utc)
            };

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => service.CreateReservation(reservationDto, customer.Id)
            );

            Assert.Empty(await context.Reservations.ToListAsync());
        }
    }
    [Fact]
    public async Task CreateReservation_WhenBarberDoesNotExist_ThrowsNotFoundException()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (_, haircut, customer) =
                await SeedTestDataAsync(context);

            var service = new ReservationService(context);

            var reservationDto = new CreateReservationDto
            {
                BarberId = -1,
                HairCutId = haircut.Id,
                StartTime = new DateTime(
                    2027, 10, 18, 7, 0, 0, DateTimeKind.Utc)
            };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => service.CreateReservation(reservationDto, customer.Id)
            );

            Assert.Empty(await context.Reservations.ToListAsync());
        }
    }
    [Fact]
public async Task CreateReservation_WhenHaircutDoesNotExist_ThrowsNotFoundException()
{
    var (context, connection) = CreateTestDatabase();

    await using (context)
    await using (connection)
    {
        // Arrange
        var (barber, _, customer) =
            await SeedTestDataAsync(context);

        var service = new ReservationService(context);

        var reservationDto = new CreateReservationDto
        {
            BarberId = barber.Id,
            HairCutId = -1,
            StartTime = new DateTime(
                2027, 10, 18, 7, 0, 0, DateTimeKind.Utc)
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.CreateReservation(reservationDto, customer.Id)
        );

        Assert.Empty(await context.Reservations.ToListAsync());
    }
}
    [Fact]
    public async Task CreateReservation_WhenStartTimeIsInPast_ThrowsBadRequestException()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var service = new ReservationService(context);

            var reservationDto = new CreateReservationDto
            {
                BarberId = barber.Id,
                HairCutId = haircut.Id,
                StartTime = DateTime.UtcNow.AddDays(-1)
            };

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => service.CreateReservation(reservationDto, customer.Id)
            );

            Assert.Empty(await context.Reservations.ToListAsync());
        }
    }
    [Fact]
    public async Task UpdateReservationAsync_WhenValid_UpdatesReservationInDatabase()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var originalStartTime = new DateTime(
                2027, 10, 18, 7, 0, 0, DateTimeKind.Utc);

            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                originalStartTime
            );

            var reservation = await context.Reservations.SingleAsync();

            var newStartTime = new DateTime(
                2027, 10, 18, 7, 30, 0, DateTimeKind.Utc);

            var updateDto = new UpdateReservationDto
            {
                StartTime = newStartTime
            };

            var service = new ReservationService(context);

            // Act
            var result = await service.UpdateReservationAsync(
                reservation.Id,
                updateDto,
                customer.Id,
                "User"
            );

            // Assert
            Assert.Equal(newStartTime, result.StartTime);
            Assert.Equal(reservation.Id, result.Id);
            Assert.Equal(customer.Id, result.UserId);

            var updatedReservation = await context.Reservations
                .AsNoTracking()
                .SingleAsync(r => r.Id == reservation.Id);

            Assert.Equal(newStartTime, updatedReservation.StartTime);
            Assert.Single(await context.Reservations.ToListAsync());
        }
    }
    [Fact]
    public async Task UpdateReservationAsync_WhenOverlapping_ThrowsConflictException()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);
            
            var secondCustomer = await CreateSecondCustomerAsync(context);

            var originalStart = new DateTime(
                2027, 10, 18, 7, 0, 0, DateTimeKind.Utc);

            var occupiedStart = new DateTime(
                2027, 10, 18, 7, 30, 0, DateTimeKind.Utc);

            await AddReservationAsync(
                context, barber, haircut, customer, originalStart);

            var reservation = await context.Reservations
                .SingleAsync(r => r.UserId == customer.Id);

            await AddReservationAsync(
                context, barber, haircut, secondCustomer, occupiedStart);

            var service = new ReservationService(context);

            var updateDto = new UpdateReservationDto
            {
                StartTime = occupiedStart
            };

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(
                () => service.UpdateReservationAsync(
                    reservation.Id,
                    updateDto,
                    customer.Id,
                    "User")
            );

            // Verify both reservations remain unchanged
            var savedReservations = await context.Reservations
                .AsNoTracking()
                .OrderBy(r => r.StartTime)
                .ToListAsync();

            Assert.Equal(2, savedReservations.Count);
            Assert.Equal(originalStart, savedReservations[0].StartTime);
            Assert.Equal(occupiedStart, savedReservations[1].StartTime);
        }
    }
    [Fact]
    public async Task UpdateReservationAsync_WhenUserIsNotOwner_ThrowsForbiddenException()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var secondCustomer = await CreateSecondCustomerAsync(context);
          
            var originalStart = new DateTime(
                2027, 10, 18, 7, 0, 0, DateTimeKind.Utc);

            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                originalStart
            );

            var reservation = await context.Reservations.SingleAsync();

            var updateDto = new UpdateReservationDto
            {
                StartTime = new DateTime(
                    2027, 10, 18, 7, 30, 0, DateTimeKind.Utc)
            };

            var service = new ReservationService(context);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(
                () => service.UpdateReservationAsync(
                    reservation.Id,
                    updateDto,
                    secondCustomer.Id,
                    "User")
            );

            // Verify reservation was not modified
            var savedReservation = await context.Reservations
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(originalStart, savedReservation.StartTime);
        }
    }
    
    [Fact]
    public async Task UpdateReservationAsync_WhenAdminIsNotOwner_UpdatesReservation()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var originalStart = new DateTime(
                2027, 10, 18, 7, 0, 0, DateTimeKind.Utc);

            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                originalStart
            );

            var reservation = await context.Reservations.SingleAsync();

            var admin = new User
            {
                Name = "Test Admin",
                Email = "admin@example.com",
                Password = "test",
                PhoneNumber = "0611111111"
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            var newStart = new DateTime(
                2027, 10, 18, 7, 30, 0, DateTimeKind.Utc);

            var updateDto = new UpdateReservationDto
            {
                StartTime = newStart
            };

            var service = new ReservationService(context);

            // Act
            var result = await service.UpdateReservationAsync(
                reservation.Id,
                updateDto,
                admin.Id,
                "Admin"
            );

            // Assert
            Assert.Equal(newStart, result.StartTime);
            Assert.Equal(customer.Id, result.UserId);

            var savedReservation = await context.Reservations
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(newStart, savedReservation.StartTime);
            Assert.Equal(customer.Id, savedReservation.UserId);
        }
    }

    [Fact]
    public async Task UpdateReservationAsync_WhenReservationDoesNotExist_ThrowsNotFoundException()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var service = new ReservationService(context);

            var updateDto = new UpdateReservationDto
            {
                StartTime = new DateTime(
                    2027, 10, 18, 7, 30, 0, DateTimeKind.Utc)
            };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateReservationAsync(
                -1,
                updateDto,
                customer.Id,
                "User")
            );

            Assert.Empty(await context.Reservations.ToListAsync());
        }
    }
    [Fact]
    public async Task CancelReservationAsync_WhenUserIsOwner_RemovesReservation()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                new DateTime(2027, 10, 18, 7, 0, 0, DateTimeKind.Utc)
            );

            var reservation = await context.Reservations.SingleAsync();

            var service = new ReservationService(context);

            // Act
            await service.CancelReservationAsync(
                reservation.Id,
                customer.Id,
                "User"
            );

            // Assert
            Assert.Empty(await context.Reservations.ToListAsync());
        }
    }
    [Fact]
    public async Task CancelReservationAsync_WhenUserIsNotOwner_ThrowsForbiddenException()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var secondCustomer = await CreateSecondCustomerAsync(context);

            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                new DateTime(2027, 10, 18, 7, 0, 0, DateTimeKind.Utc)
            );

            var reservation = await context.Reservations.SingleAsync();
            var service = new ReservationService(context);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(
                () => service.CancelReservationAsync(
                    reservation.Id,
                    secondCustomer.Id,
                    "User")
            );

            // Reservation must still exist
            var savedReservation = await context.Reservations
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(reservation.Id, savedReservation.Id);
            Assert.Equal(customer.Id, savedReservation.UserId);
        }
    }
    [Fact]
    public async Task GetMyReservationsAsync_ReturnsOnlyOwnReservations()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var secondCustomer = await CreateSecondCustomerAsync(context);

            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                new DateTime(2027, 10, 18, 7, 0, 0, DateTimeKind.Utc)
            );

            await AddReservationAsync(
                context,
                barber,
                haircut,
                secondCustomer,
                new DateTime(2027, 10, 18, 7, 30, 0, DateTimeKind.Utc)
            );

            var service = new ReservationService(context);

            // Act
            var result = await service.GetMyReservationsAsync(customer.Id);

            // Assert
            Assert.Single(result);
            Assert.Equal(customer.Id, result[0].UserId);
            Assert.Equal(
                new DateTime(2027, 10, 18, 7, 0, 0, DateTimeKind.Utc),
                result[0].StartTime
            );
        }
    }
    [Fact]
    public async Task GetMyReservationsAsync_WhenUserHasNoReservations_ReturnsEmptyList()
    {
        var (context, connection) = CreateTestDatabase();

        await using (context)
        await using (connection)
        {
            // Arrange
            var (barber, haircut, customer) =
                await SeedTestDataAsync(context);

            var secondCustomer = await CreateSecondCustomerAsync(context);

            await AddReservationAsync(
                context,
                barber,
                haircut,
                customer,
                new DateTime(2027, 10, 18, 7, 0, 0, DateTimeKind.Utc)
            );

            var service = new ReservationService(context);

            // Act
            var result = await service.GetMyReservationsAsync(secondCustomer.Id);

            // Assert
            Assert.Empty(result);
        }
    }
    
}