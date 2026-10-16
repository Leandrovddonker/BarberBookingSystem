using planeProject.Common;

namespace planeProject.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public UserRole Role { get; set; }
    public string PhoneNumber { get; set; }
    public Barber? Barber { get; set; }
    public List<Reservation> Reservations { get; set; }

}