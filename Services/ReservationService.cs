using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using planeProject.Data;
using planeProject.Exceptions;
using planeProject.Common;
using planeProject.Models;
using planeProject.Models.Dtos;

namespace planeProject.Services;

public class ReservationService(PlaneDbContext context) : IReservationService
{
    public async Task<ReservationDto> CreateReservation(CreateReservationDto reservationDto, int userId)
    {
        var barber = await context.Barbers.FirstOrDefaultAsync(b => b.Id == reservationDto.BarberId);
        var haircut = await context.HairCuts.FirstOrDefaultAsync(h => h.Id == reservationDto.HairCutId);

        if (barber == null || haircut == null)
        {
            throw new NotFoundException("Barber or haircut not found.");
        }
        
        var newResStart = reservationDto.StartTime;
        var newResEnd = newResStart.AddMinutes(haircut.DurationMinutes);
        await DoesReservationMatchCriteria(barber.Id, newResStart, newResEnd);
        
        var reservation = new Reservation
        {
            BarberId = barber.Id,
            HaircutId = haircut.Id,
            StartTime = newResStart,
            UserId = userId,
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();
        return new ReservationDto()
        {
            Id = reservation.Id,
            BarberId = barber.Id,
            HairCutId = haircut.Id,
            StartTime = newResStart,
            UserId = userId,
        };
    }

    public async Task CancelReservationAsync(int reservationId, int userId, string role)
    {
        var reservation = await GetAuthorizedReservationAsync(reservationId, userId, role);
        context.Reservations.Remove(reservation);
        await context.SaveChangesAsync();
    }
    
    public async Task<ReservationDto> UpdateReservationAsync(int reservationId, UpdateReservationDto reservationDto,
        int userId, string role)
    {
        var reservation = await GetAuthorizedReservationAsync(reservationId, userId, role);
        
        var newResStart = reservationDto.StartTime;
        var resEnd = newResStart.AddMinutes(reservation.HairCut.DurationMinutes);
        
        await DoesReservationMatchCriteria(reservation.BarberId, newResStart, resEnd, reservation.Id);
        
        reservation.StartTime = newResStart;
        await context.SaveChangesAsync();
        return new ReservationDto()
        {
            Id = reservation.Id,
            UserId = reservation.UserId,
            BarberId = reservation.BarberId,
            HairCutId = reservation.HaircutId,
            StartTime = newResStart
        };
    }

    public async Task<List<ReservationDto>> GetMyReservationsAsync(int userId)
    {
        var reservations = await context.Reservations
            .Where(r => r.UserId == userId).ToListAsync();
        return GetReservations(reservations);
    }

    public async Task<List<ReservationDto>> GetAllReservationsAsync()
    {
        var reservations = await context.Reservations.ToListAsync();
        return GetReservations(reservations);
    }

    public async Task<List<BarberReservationDto>> GetBarberReservationsAsync(int userId)
    {
        var barber = await context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
        if (barber == null)
        {
            throw new NotFoundException("Barber not found.");
        }
        var reservations = await context.Reservations
            .Where(r => r.BarberId == barber.Id)
            .Include(r=> r.User)
            .Include(r=> r.HairCut)
            .OrderBy(r => r.StartTime)
            .ToListAsync();
        
        var listOfBarberReservations = new List<BarberReservationDto>();
        foreach (var reservation in reservations)
        {
            var singleBarberReservation = new BarberReservationDto()
            {
                Id = reservation.Id,
                CustomerName = reservation.User.Name,
                PhoneNumber = reservation.User.PhoneNumber,
                HaircutName = reservation.HairCut.Cut,
                Price = reservation.HairCut.Price,
                DurationMinutes = reservation.HairCut.DurationMinutes,
                StartTime = reservation.StartTime,
                EndTime = reservation.StartTime.AddMinutes(reservation.HairCut.DurationMinutes)
            };
            listOfBarberReservations.Add(singleBarberReservation);
        }
        return listOfBarberReservations;
    }

    public async Task<List<DateTime>> GetAvailableSlotsAsync(int barberId, int haircutId, DateOnly date)
    {
        var openingHours = await context.OpeningHours
            .FirstOrDefaultAsync(h => h.Day == date.DayOfWeek);
        var haircut = await context.HairCuts.FirstOrDefaultAsync(h => h.Id == haircutId);
        var barberExists = await context.Barbers.FirstOrDefaultAsync(b => b.Id == barberId);
        if (barberExists == null)
        {
            throw new NotFoundException("Barber not found.");
        }
        if (haircut == null)
        {
            throw new NotFoundException("Haircut not found.");
        }
        if (openingHours == null || openingHours.OpeningTime >= openingHours.ClosingTime)
        {
            return [];
        }
        return await GenerateAvailableSlotsAsync(barberId,haircut.DurationMinutes, date, openingHours);
    }

    private async Task<List<DateTime>> GenerateAvailableSlotsAsync(
        int barberId, int durationMinutes, DateOnly date, OpeningHours openingHours)
    {
        var availableSlots = new List<DateTime>();
        var currentTime = date.ToDateTime(
            TimeOnly.FromTimeSpan(openingHours.OpeningTime));
        var closingTime = date.ToDateTime(
            TimeOnly.FromTimeSpan(openingHours.ClosingTime));
        
        const int slotIntervalMinutes = 15;
        var dutchTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        var reservations = await GetReservationsForDayAsync(barberId, date);
        while (currentTime.AddMinutes(durationMinutes) <= closingTime)
        {
            var utcStartTime = TimeZoneInfo.ConvertTimeToUtc(
                currentTime, dutchTimeZone);

            var utcEndTime = utcStartTime.AddMinutes(durationMinutes);

            var hasOverlap = reservations.Any(r =>
                utcStartTime < r.StartTime.AddMinutes(r.HairCut.DurationMinutes) &&
                utcEndTime > r.StartTime);

            if (utcStartTime > DateTime.UtcNow && !hasOverlap)
            {
                availableSlots.Add(utcStartTime);
            }
            currentTime = currentTime.AddMinutes(slotIntervalMinutes);
        }
        return availableSlots;
    }

    //Could use HasOverLappingReservationAsync but to prevent a query overload due to while loop, i made this helper function
    private async Task<List<Reservation>> GetReservationsForDayAsync(int barberId, DateOnly date)
    {
        var dutchTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            date.ToDateTime(TimeOnly.MinValue), dutchTimeZone);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(date.AddDays(1)
            .ToDateTime(TimeOnly.MinValue), dutchTimeZone);
        
        return await context.Reservations
            .AsNoTracking()
            .Include(r => r.HairCut)
            .Where(r => r.BarberId == barberId &&
                        r.StartTime >= dayStartUtc &&
                        r.StartTime < dayEndUtc)
            .ToListAsync();
    }
    
    private static List<ReservationDto> GetReservations(List<Reservation> reservations)
    {
        var listOfReservations = new List<ReservationDto>();
        foreach (var reservation in reservations)
        {
            var singleReservation = new ReservationDto()
            {
                Id = reservation.Id,
                BarberId = reservation.BarberId,
                HairCutId = reservation.HaircutId,
                UserId = reservation.UserId,
                StartTime = reservation.StartTime,
            };
            listOfReservations.Add(singleReservation);
        }
        return listOfReservations;
    }
    
    private async Task<Reservation> GetAuthorizedReservationAsync(int reservationId, int userId, string role)
    {
        var reservation = await context.Reservations
            .Include(r => r.HairCut)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
        {
            throw new NotFoundException("Reservation not found.");
        }

        if (reservation.UserId != userId && role != "Admin")
        {
            throw new ForbiddenException("You are not allowed to update this reservation.");
        }
        return reservation;
    }
    
    private async Task DoesReservationMatchCriteria(int barberId, DateTime startTime, 
        DateTime endTime, int? excludeReservationId = null)
    {
        if (startTime <= DateTime.UtcNow)
        {
            throw new BadRequestException("The reservation is in the past.");
        }
        if (!await IsWithinOpeningHoursAsync(startTime, endTime))
        {
            throw new BadRequestException("Reservation is outside opening hours.");
        }
        
        if (await HasOverLappingReservationAsync(barberId, startTime, endTime, excludeReservationId))
        {
            throw new ConflictException("This barber is already booked at the selected time.");
        }
    }

    private async Task<bool> IsWithinOpeningHoursAsync(DateTime newResStart, DateTime newResEnd)
    {
        var dutchTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(newResStart, dutchTimeZone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(newResEnd, dutchTimeZone);
        
        var dayOfWeek = localStart.DayOfWeek;
        var openingHours = await context.OpeningHours.FirstOrDefaultAsync(h => h.Day == dayOfWeek);
        if (openingHours == null)
        {
            return false;
        }

        var reservationStart = localStart.TimeOfDay;
        var reservationEnd = localEnd.TimeOfDay;
        
        var openingTime = openingHours.OpeningTime;
        var closingTime = openingHours.ClosingTime;

        if (reservationStart < openingTime || reservationEnd > closingTime || localStart.Date != localEnd.Date)
        {
            return false;
        }
        return true;
    }
    
    private async Task<bool> HasOverLappingReservationAsync(int barberId, DateTime newResStart, 
        DateTime newResEnd, int? excludeReservationId = null)
    {
        var barbersReservation = await context.Reservations
            .Where(r=> r.BarberId == barberId && r.Id != excludeReservationId)
            .Include(r=> r.HairCut)
            .ToListAsync();

        foreach (var existingReservation in barbersReservation)
        {
            var endTime = existingReservation.StartTime.AddMinutes(existingReservation.HairCut.DurationMinutes);
            if (newResStart < endTime && newResEnd > existingReservation.StartTime)
            {
                return true;
            }
        }
        return false;
    }
}