namespace BarberBookingSystem.Models.Dtos;

public class CreateSpecialOpeningHoursDto
{
    public DateOnly Date { get; set; }
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }
    public bool IsClosed { get; set; }
}