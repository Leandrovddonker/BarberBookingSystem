using BarberBookingSystem.Common;
using BarberBookingSystem.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using BarberBookingSystem.Models;


namespace BarberBookingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> CreateUserAsync(CreateUserDto user)
    {
        var newUser = await authService.RegisterAsync(user);
        return StatusCode(201, newUser);
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        var token = await authService.LoginAsync(loginDto);
        return StatusCode(201, token);
    }
}