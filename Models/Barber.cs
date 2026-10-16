namespace planeProject.Models;

public class Barber
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    
    public string Name { get; set; }
    public string? Description { get; set; }

    public List<Reservation> Reservations { get; set; } = new();

}