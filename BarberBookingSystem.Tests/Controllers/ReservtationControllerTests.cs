using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BarberBookingSystem.Tests.Controllers;

public class ReservationControllerTests
{
    private static string GenerateJwtToken(
        string key,
        string issuer,
        string audience,
        int userId,
        string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    [Fact]
    public async Task GetMyReservations_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Reservation/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task GetMyReservations_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "invalid.jwt.token"
            );

        // Act
        var response = await client.GetAsync("/api/Reservation/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task GetMyReservations_WithValidCustomerToken_ReturnsOk()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Jwt:Key"] = "ThisIsATestSigningKeyWithAtLeast32Characters!",
                            ["Jwt:Issuer"] = "BarberBookingSystem.Tests",
                            ["Jwt:Audience"] = "BarberBookingSystem.Tests"
                        });
                });
            });

        using var client = factory.CreateClient();

        var token = GenerateJwtToken(
            "ThisIsATestSigningKeyWithAtLeast32Characters!",
            "BarberBookingSystem.Tests",
            "BarberBookingSystem.Tests",
            1,
            "Customer"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/Reservation/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task GetMyReservations_WithWrongRole_ReturnsForbidden()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Jwt:Key"] = "ThisIsATestSigningKeyWithAtLeast32Characters!",
                            ["Jwt:Issuer"] = "BarberBookingSystem.Tests",
                            ["Jwt:Audience"] = "BarberBookingSystem.Tests"
                        });
                });
            });

        using var client = factory.CreateClient();

        var token = GenerateJwtToken(
            "ThisIsATestSigningKeyWithAtLeast32Characters!",
            "BarberBookingSystem.Tests",
            "BarberBookingSystem.Tests",
            1,
            "Admin"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/Reservation/me");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}