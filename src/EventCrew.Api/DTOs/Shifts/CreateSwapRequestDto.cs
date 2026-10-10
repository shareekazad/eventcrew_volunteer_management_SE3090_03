using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Shifts;

public class CreateSwapRequestDto
{
    [Required]
    public Guid RequesterAssignmentId { get; set; }

    [Required]
    public Guid TargetVolunteerId { get; set; }

    [Required]
    public Guid TargetShiftId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}