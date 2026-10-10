using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

/// <summary>
/// A volunteer's request to swap one of their shift assignments with another volunteer.
/// Matches the shift_swap_requests table.
/// </summary>
[Table("shift_swap_requests")]
public class ShiftSwapRequest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("requester_assignment_id")]
    public Guid RequesterAssignmentId { get; set; }

    [Required]
    [Column("target_volunteer_id")]
    public Guid TargetVolunteerId { get; set; }

    [Required]
    [Column("target_shift_id")]
    public Guid TargetShiftId { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [Required]
    [MaxLength(25)]
    [Column("status")]
    public string Status { get; set; } = "Pending_Target";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ---- Navigation ----
    [ForeignKey(nameof(RequesterAssignmentId))]
    public ShiftAssignment RequesterAssignment { get; set; } = null!;

    [ForeignKey(nameof(TargetVolunteerId))]
    public VolunteerProfile TargetVolunteer { get; set; } = null!;

    [ForeignKey(nameof(TargetShiftId))]
    public Shift TargetShift { get; set; } = null!;
}