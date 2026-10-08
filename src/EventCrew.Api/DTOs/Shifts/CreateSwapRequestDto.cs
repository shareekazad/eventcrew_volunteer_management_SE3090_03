using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Shifts;

/// <summary>Request body for creating a shift swap request.</summary>
public class CreateSwapRequestDto
{
    /// <summary>
    /// The ID of the requester's ShiftAssignment they want to swap away.
    /// </summary>
    [Required]
    public Guid RequesterAssignmentId { get; set; }

    /// <summary>
    /// The VolunteerProfile ID of the volunteer to swap with.
    /// </summary>
    [Required]
    public Guid TargetVolunteerId { get; set; }

    /// <summary>
    /// The shift (assignment) the target volunteer holds that the requester wants.
    /// This is the ShiftAssignment.ShiftId belonging to the target volunteer.
    /// </summary>
    [Required]
    public Guid TargetShiftId { get; set; }

    /// <summary>Optional explanation for the swap request.</summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }
}
