namespace BarberBookingSystem.Models;

public class HairCut
{
    public int Id { get; set; }
    public string Cut { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public List<Reservation> Reservations { get; set; } = new();
}