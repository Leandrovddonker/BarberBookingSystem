namespace planeProject.Models.Dtos;

public class HairCutDto
{
    public string Cut { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
}