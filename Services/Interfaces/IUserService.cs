using BarberBookingSystem.Models.Dtos;
using BarberBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace BarberBookingSystem.Common;

public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(int id);
    Task<List<UserDto>> GetAllUsersAsync();
    // Task<User> UpdateUserByIdAsync(User user, int id);
    // Task<UserDto> CreateUserAsync(CreateUserDto user);
    // Task<User> DeleteUserByIdAsync(int id);
}