using BarberBookingSystem.Models.Dtos;
using BarberBookingSystem.Models;

namespace BarberBookingSystem.Common;

public interface IReservationService
{
    Task<ReservationDto> CreateReservation(CreateReservationDto reservationDto, int userId);
    Task CancelReservationAsync(int reservationId, int userId, string role);
    Task<ReservationDto> UpdateReservationAsync(int reservationId, UpdateReservationDto reservationDto, int userId, string role);
    Task<List<ReservationDto>> GetMyReservationsAsync(int userId);
    Task<List<BarberReservationDto>> GetBarberReservationsAsync(int barberId);
    Task<List<ReservationDto>> GetAllReservationsAsync();
    Task<List<DateTime>> GetAvailableSlotsAsync(int barberId, int haircutId, DateOnly date);
}