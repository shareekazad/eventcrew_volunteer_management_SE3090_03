using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

public static class ShiftSwapStatus
{
    public const string PendingTarget = "Pending_Target";
    public const string PendingOrganizer = "Pending_Organizer";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}

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
    public string Status { get; set; } = ShiftSwapStatus.PendingTarget;

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
