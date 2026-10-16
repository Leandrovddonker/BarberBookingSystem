using BarberBookingSystem.Models.Dtos;
using BarberBookingSystem.Models;

namespace BarberBookingSystem.Common;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
    Task<UserDto> RegisterAsync(CreateUserDto createUserDto);
}