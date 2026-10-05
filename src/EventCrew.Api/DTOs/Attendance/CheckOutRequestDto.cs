using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Attendance;

public class CheckOutRequestDto
{
    [Required]
    public Guid VolunteerId { get; set; }
}
