namespace BarberBookingSystem.Models.Dtos;

public class BarberReservationDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public string PhoneNumber {get; set;}
    public string HaircutName { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}