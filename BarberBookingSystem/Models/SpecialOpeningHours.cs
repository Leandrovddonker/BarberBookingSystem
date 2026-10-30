namespace BarberBookingSystem.Models;

public class SpecialOpeningHours
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }
    public bool IsClosed { get; set; }
}