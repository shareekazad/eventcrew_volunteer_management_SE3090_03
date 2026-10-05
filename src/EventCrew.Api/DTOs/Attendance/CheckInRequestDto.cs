using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Attendance;

public class CheckInRequestDto
{
    [Required]
    public Guid VolunteerId { get; set; }

    [Required]
    public Guid EventId { get; set; }

    [Required]
    public Guid ShiftId { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 32)]
    public string Token { get; set; } = string.Empty;
}
