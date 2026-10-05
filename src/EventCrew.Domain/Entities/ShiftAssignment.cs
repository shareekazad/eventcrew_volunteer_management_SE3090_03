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

    public Shift Shift { get; set; } = null!;
    public AttendanceRecord? AttendanceRecord { get; set; }
}
