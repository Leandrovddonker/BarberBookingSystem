namespace BarberBookingSystem.Models.Dtos;

public class ReservationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BarberId { get; set; }
    public int HairCutId { get; set; }
    public DateTime StartTime { get; set; }
}