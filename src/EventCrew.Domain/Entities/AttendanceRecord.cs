using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventCrew.Domain.Enums;

namespace EventCrew.Domain.Entities;

[Table("attendance_records")]
public class AttendanceRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("shift_assignment_id")]
    public Guid ShiftAssignmentId { get; set; }

    [Column("check_in_time")]
    public DateTimeOffset? CheckInTime { get; set; }

    [Column("check_out_time")]
    public DateTimeOffset? CheckOutTime { get; set; }

    [Column("status")]
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Pending;

    [Column("verified_hours", TypeName = "numeric(5,2)")]
    public decimal VerifiedHours { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ShiftAssignment ShiftAssignment { get; set; } = null!;
}
