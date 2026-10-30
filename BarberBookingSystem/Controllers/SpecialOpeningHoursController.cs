using BarberBookingSystem.Models.Dtos;
using BarberBookingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BarberBookingSystem.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SpecialOpeningHoursController(ISpecialOpeningHoursService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var hours = await service.GetAllAsync();
        return Ok(hours);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var hours = await service.GetByIdAsync(id);

        if (hours == null)
            return NotFound();

        return Ok(hours);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateSpecialOpeningHoursDto dto)
    {
        var hours = await service.CreateAsync(dto);

        return StatusCode(201, hours);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id,
        [FromBody] UpdateSpecialOpeningHoursDto dto)
    {
        var hours = await service.UpdateAsync(id, dto);

        if (hours == null)
            return NotFound();

        return Ok(hours);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var deleted = await service.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpGet("reservation-count")]
    public async Task<IActionResult> GetReservationCountAsync(
        [FromQuery] DateOnly date)
    {
        var count = await service.GetReservationCountAsync(date);
        return Ok(count);
    }
}
