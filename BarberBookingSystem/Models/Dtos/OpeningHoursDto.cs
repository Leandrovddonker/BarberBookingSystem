using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BarberBookingSystem.Models.Dtos;

public class OpeningHoursDto
{
    public DayOfWeek Day { get; set; }
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
}