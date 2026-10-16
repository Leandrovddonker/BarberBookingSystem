using planeProject.Models;
using planeProject.Models.Dtos;

namespace planeProject.Common;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
    Task<UserDto> RegisterAsync(CreateUserDto createUserDto);
}