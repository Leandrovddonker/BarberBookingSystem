namespace planeProject.Models.Dtos;

public class CreateUserDto
{
    public string Name { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
    public string PhoneNumber { get; set; } = String.Empty;
}