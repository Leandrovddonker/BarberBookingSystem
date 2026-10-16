using Microsoft.EntityFrameworkCore;
using planeProject.Data;
using planeProject.Common;
using planeProject.Models;
using planeProject.Models.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using planeProject.Exceptions;

namespace planeProject.Services;

public class AuthService(PlaneDbContext context, IConfiguration configuration) : IAuthService
{
    public async Task<UserDto> RegisterAsync(CreateUserDto user)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(
            u => u.Email == user.Email);
        if (existingUser != null)
        {
            throw new ConflictException("Email already exists");
        }
        
        var newUser = new User()
        {
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Password = BCrypt.Net.BCrypt.HashPassword(user.Password),
            Role = UserRole.Customer
        };
        context.Users.Add(newUser);
        await context.SaveChangesAsync();
        return new UserDto()
        {
            Id = newUser.Id,
            Name = newUser.Name,
            Email = newUser.Email,
            PhoneNumber = newUser.PhoneNumber,
            Role = UserRole.Customer
        };
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await context.Users.FirstOrDefaultAsync(
            u => u.Email == loginDto.Email);

        if (user == null)
        {
            throw new NotFoundException("User not found");
        }
        
        var passwordCorrect = BCrypt.Net.BCrypt.Verify(
            loginDto.Password,
            user.Password);

        if (!passwordCorrect)
        {
            throw new ConflictException("Password doesn't match");
        }
        
        var response = GenerateToken(user);
        
        return response;
    }

    private LoginResponseDto GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var jwtToken = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"], 
            claims: claims, expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(jwtToken);

        var response = new LoginResponseDto()
        {
            Token = tokenString
        };
        return response;
    }
    
}