namespace planeProject.Models;

public class OpeningHours
{
    public int Id { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeSpan OpeningTime { get; set; }
    public TimeSpan ClosingTime { get; set; }
    
}