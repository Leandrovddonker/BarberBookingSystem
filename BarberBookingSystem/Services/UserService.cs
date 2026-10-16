using BarberBookingSystem.Common;
using BarberBookingSystem.Data;
using BarberBookingSystem.Exceptions;
using BarberBookingSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using BarberBookingSystem.Models;

namespace BarberBookingSystem.Services;

public class UserService(BarberSystemDbContext context) : IUserService
{
    
    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var user = await context.Users
            .Include(u => u.Reservations)
            .FirstOrDefaultAsync(u => u.Id == id);
        
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        var userDto = new UserDto()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Reservations = user.Reservations

        };
        return userDto;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await context.Users.ToListAsync();
        var listOfUserDto = new List<UserDto>();
        foreach (var user in users)
        {
            var userDto = new UserDto()
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Reservations = user.Reservations,
                Role = user.Role,
            };
            listOfUserDto.Add(userDto);
        }
        return listOfUserDto;
    }
}