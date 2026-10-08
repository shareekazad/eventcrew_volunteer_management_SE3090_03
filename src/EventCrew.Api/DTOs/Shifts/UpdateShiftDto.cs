using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Shifts;

public class UpdateShiftDto
{
    public Guid? RoleRequirementId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTimeOffset StartTime { get; set; }

    [Required]
    public DateTimeOffset EndTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than zero.")]
    public int Capacity { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }
}
