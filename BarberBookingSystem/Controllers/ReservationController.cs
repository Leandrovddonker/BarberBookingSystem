using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BarberBookingSystem.Services.Interfaces;
using BarberBookingSystem.Models.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace BarberBookingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationController(IReservationService reservationService) : ControllerBase
{
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> CreateReservation(CreateReservationDto reservationDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }
        
        var newReservation = await reservationService.CreateReservation(reservationDto, userId);
        return StatusCode(201, newReservation);
    }

    [Authorize(Roles = "Admin,Customer")]
    [HttpDelete("{reservationId}")]
    public async Task<IActionResult> CancelReservation(int reservationId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!int.TryParse(userIdClaim, out int userId) || role == null)
        {
            return Unauthorized();
        }
        await reservationService.CancelReservationAsync(reservationId,userId, role);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Customer")]
    [HttpPut("{reservationId}")]
    public async Task<IActionResult> UpdateReservation(int reservationId, UpdateReservationDto reservationDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!int.TryParse(userIdClaim, out int userId) || role == null)
        {
            return Unauthorized();
        }
        var updatedReservation = await reservationService.UpdateReservationAsync(reservationId, reservationDto, userId, role);
        return Ok(updatedReservation);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyReservationsAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }
        var listOfReservations = await reservationService.GetMyReservationsAsync(userId);
        return Ok(listOfReservations);
    }

    [Authorize(Roles = "Barber")]
    [HttpGet("barber/me")]
    public async Task<IActionResult> GetBarberReservationsAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }
        var listOfReservations = await reservationService.GetBarberReservationsAsync(userId);
        return Ok(listOfReservations);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllReservationsAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }
        var listOfReservations = await reservationService.GetAllReservationsAsync();
        return Ok(listOfReservations);
    }

    [HttpGet("available-slots")]
    public async Task<ActionResult<List<DateTime>>> GetAvailableSlots(
        [FromQuery] int barberId,
        [FromQuery] int haircutId,
        [FromQuery] DateOnly date)
    {
        var slots = await reservationService.GetAvailableSlotsAsync(
            barberId, haircutId, date);

        return Ok(slots);
    }
}