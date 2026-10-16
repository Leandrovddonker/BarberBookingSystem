using Microsoft.EntityFrameworkCore;
using planeProject.Data;
using planeProject.Exceptions;
using planeProject.Common;
using planeProject.Models;
using planeProject.Models.Dtos;

namespace planeProject.Services;

public class BarberService(PlaneDbContext context) : IBarberService
{
    public async Task<BarberDto> CreateBarberAsync(CreateBarberDto createBarberDto)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == createBarberDto.Email);
        if (existingUser != null)
        {
            throw new ConflictException("Email already exists");
        }

        var user = new User()
        {
            Name = createBarberDto.Name,
            Email = createBarberDto.Email,
            Role = UserRole.Barber,
            PhoneNumber = createBarberDto.PhoneNumber,
            Password = BCrypt.Net.BCrypt.HashPassword(createBarberDto.Password),
        };
        context.Users.Add(user);

        var barber = new Barber()
        {
            User = user,
            Name = createBarberDto.Name,
            Description = createBarberDto.Description,
        };
        context.Barbers.Add(barber);
        await context.SaveChangesAsync();
        return new BarberDto()
        {
            Name = createBarberDto.Name,
            Email = createBarberDto.Email,
            PhoneNumber = createBarberDto.PhoneNumber,
            Description = createBarberDto.Description,
        };

    }

    public async Task<BarberDto> GetBarberByIdAsync(int id)
    {
        var barber = await context.Barbers
            .Include(b => b.User.Reservations)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (barber == null)
        {
            throw new NotFoundException("Barber not found");
        }

        return new BarberDto()
        {
            Id = barber.Id,
            Name = barber.Name,
            Description = barber.Description,
            Email = barber.User.Email,
            PhoneNumber = barber.User.PhoneNumber,
            UserId = barber.User.Id,
        };
    }

    public async Task<List<BarberDto>> GetAllBarbersAsync()
    {
        var barbers = await context.Barbers
            .Include(b => b.User) 
            .ToListAsync();
        var listOfBarberDtos = new List<BarberDto>();
        foreach (var barber in barbers)
        {
            var barberDto = new BarberDto()
            {
                Id = barber.Id,
                Name = barber.Name,
                Description = barber.Description,
                Email = barber.User.Email,
                PhoneNumber = barber.User.PhoneNumber,
                UserId = barber.User.Id,
            };
            listOfBarberDtos.Add(barberDto);
        }
        return listOfBarberDtos;
    }
}