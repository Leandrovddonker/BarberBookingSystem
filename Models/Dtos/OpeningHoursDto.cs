using System.ComponentModel;
using System.Text.Json.Serialization;

namespace planeProject.Models.Dtos;

public class OpeningHoursDto
{
    public DayOfWeek Day { get; set; }
    public string OpeningTime { get; set; } = "00:00:00";
    public string ClosingTime { get; set; } = "00:00:00";
}