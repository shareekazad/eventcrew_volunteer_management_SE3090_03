using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

[Table("shift_assignments")]
public class ShiftAssignment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("shift_id")]
    public Guid ShiftId { get; set; }

    [Required]
    [Column("volunteer_id")]
    public Guid VolunteerId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Proposed_By_AI";

    [Column("assigned_by_user_id")]
    public Guid? AssignedByUserId { get; set; }

    [Column("assigned_at")]
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ---- Navigation ----
    [ForeignKey(nameof(ShiftId))]
    public Shift Shift { get; set; } = null!;

    [ForeignKey(nameof(VolunteerId))]
    public VolunteerProfile Volunteer { get; set; } = null!;

    public AttendanceRecord? AttendanceRecord { get; set; }

    public ICollection<ShiftSwapRequest> SwapRequestsAsRequester { get; set; } = new List<ShiftSwapRequest>();
}