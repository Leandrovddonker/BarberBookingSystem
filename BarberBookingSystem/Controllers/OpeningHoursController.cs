using BarberBookingSystem.Services.Interfaces;
using BarberBookingSystem.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace BarberBookingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OpeningHoursController(IOpeningHoursService openingHoursService) : ControllerBase
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAllAsync()
    {
        var openingHours = await openingHoursService.GetAllAsync();
        return Ok(openingHours);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddOpeningHoursAsync([FromBody] OpeningHoursDto openingHours)
    {
        var addOpeningHours = await openingHoursService.AddOpeningHoursAsync(openingHours);
        return StatusCode(201, addOpeningHours);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{day}")]
    public async Task<IActionResult> UpdateOpeningHoursAsync(DayOfWeek day,[FromBody] OpeningHoursDto openingHours)
    {
        var editOpeningHours = await openingHoursService.UpdateOpeningHoursByDayAsync(day, openingHours);
        return Ok(editOpeningHours);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOpeningHoursByIdAsync(int id)
    {
        await openingHoursService.DeleteOpeningHoursByIdAsync(id);
        return NoContent();
    }
    
}