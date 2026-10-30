namespace BarberBookingSystem.Models.Dtos;

public class SpecialOpeningHoursResponseDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }
    public bool IsClosed { get; set; }
}