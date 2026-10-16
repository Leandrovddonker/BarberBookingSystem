using BarberBookingSystem.Common;

namespace BarberBookingSystem.Models.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public string PhoneNumber { get; set; } = String.Empty;

    public List<Reservation> Reservations { get; set; } = new();
    
    public UserRole Role { get; set; }
}