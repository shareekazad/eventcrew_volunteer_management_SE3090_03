using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Attendance;

public class CreateQrTokenRequestDto
{
    [Required]
    public Guid ShiftId { get; set; }

    [Required]
    public DateTimeOffset ExpiresAt { get; set; }
}
