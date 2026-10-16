namespace planeProject.Models.Dtos;

public class BarberDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string? Description { get; set; }
    public int UserId { get; set; }
}