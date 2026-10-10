using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Shifts;

public class CreateShiftDto
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public Guid RoleRequirementId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset StartTime { get; set; }

    [Required]
    public DateTimeOffset EndTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0.")]
    public int Capacity { get; set; }
}