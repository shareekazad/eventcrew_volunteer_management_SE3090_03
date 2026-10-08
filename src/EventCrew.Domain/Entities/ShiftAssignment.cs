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
    public string Status { get; set; } = "Confirmed";

    [Column("assigned_by_user_id")]
    public Guid? AssignedByUserId { get; set; }

    [Column("assigned_at")]
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ShiftId))]
    public Shift Shift { get; set; } = null!;

    [ForeignKey(nameof(VolunteerId))]
    public VolunteerProfile Volunteer { get; set; } = null!;

    [ForeignKey(nameof(AssignedByUserId))]
    public User? AssignedByUser { get; set; }

    public AttendanceRecord? AttendanceRecord { get; set; }
}
