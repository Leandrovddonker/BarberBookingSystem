namespace BarberBookingSystem.Models.Dtos;

public class UpdateSpecialOpeningHoursDto
{
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }
    public bool IsClosed { get; set; }
}