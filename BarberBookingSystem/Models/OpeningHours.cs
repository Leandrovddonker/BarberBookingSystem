namespace BarberBookingSystem.Models;

public class OpeningHours
{
    public int Id { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
    
}