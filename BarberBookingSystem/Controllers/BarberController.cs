using BarberBookingSystem.Services.Interfaces;
using BarberBookingSystem.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberBookingSystem.Controllers;
[ApiController]
[Route("api/[controller]")]
public class BarberController(IBarberService barberService) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost("RegisterBarber")]
    public async Task<IActionResult> RegisterBarberAsync(CreateBarberDto barberDto)
    {
        var newBarber = await barberService.CreateBarberAsync(barberDto);
        return StatusCode(201, newBarber);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetBarberByIdAsync(int id)
    {
        var barber = await barberService.GetBarberByIdAsync(id);
        return Ok(barber);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllBarbersAsync()
    {
        var barbers = await barberService.GetAllBarbersAsync();
        return Ok(barbers);
    }
}