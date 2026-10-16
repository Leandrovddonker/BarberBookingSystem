using Microsoft.EntityFrameworkCore;
using planeProject.Data;
using planeProject.Exceptions;
using planeProject.Common;
using planeProject.Models;
using planeProject.Models.Dtos;

namespace planeProject.Services;

public class UserService(PlaneDbContext context) : IUserService
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