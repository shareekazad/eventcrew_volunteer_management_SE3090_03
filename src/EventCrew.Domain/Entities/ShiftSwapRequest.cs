using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

/// <summary>
/// Represents a volunteer's request to swap their shift assignment with another volunteer.
/// Maps to the <c>shift_swap_requests</c> table.
/// </summary>
[Table("shift_swap_requests")]
public class ShiftSwapRequest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The assignment the requesting volunteer wants to give away.</summary>
    [Required]
    [Column("requester_assignment_id")]
    public Guid RequesterAssignmentId { get; set; }

    /// <summary>The volunteer the requester wants to swap with.</summary>
    [Required]
    [Column("target_volunteer_id")]
    public Guid TargetVolunteerId { get; set; }

    /// <summary>
    /// The shift the target volunteer is currently on that the requester wants.
    /// Per schema: target_shift_id references shifts(id).
    /// </summary>
    [Required]
    [Column("target_shift_id")]
    public Guid TargetShiftId { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Status lifecycle: Pending_Target → Pending_Organizer → Approved | Rejected | Cancelled
    /// </summary>
    [Required]
    [MaxLength(25)]
    [Column("status")]
    public string Status { get; set; } = "Pending_Target";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(RequesterAssignmentId))]
    public ShiftAssignment RequesterAssignment { get; set; } = null!;

    [ForeignKey(nameof(TargetVolunteerId))]
    public VolunteerProfile TargetVolunteer { get; set; } = null!;

    [ForeignKey(nameof(TargetShiftId))]
    public Shift TargetShift { get; set; } = null!;
}
