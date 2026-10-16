namespace BarberBookingSystem.Models;

public class Reservation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    
    public int BarberId { get; set; }
    public Barber Barber { get; set; }
    public int HaircutId { get; set; }
    public HairCut HairCut { get; set; }
    public DateTime StartTime { get; set; }
}

