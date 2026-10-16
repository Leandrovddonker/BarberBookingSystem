namespace planeProject.Models.Dtos;

public class CreateReservationDto
{
    public int BarberId { get; set; }
    public int HairCutId { get; set; }
    public DateTime StartTime { get; set; }
}