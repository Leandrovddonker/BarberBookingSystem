using BarberBookingSystem.Services.Interfaces;

namespace BarberBookingSystem.Models.Dtos;

public class CreateBarberDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Email { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
    public string PhoneNumber { get; set; } = String.Empty;

}