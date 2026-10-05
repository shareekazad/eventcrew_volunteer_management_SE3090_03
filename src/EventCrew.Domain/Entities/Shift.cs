using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

[Table("shifts")]
public class Shift
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("event_id")]
    public Guid EventId { get; set; }

    [Required]
    [Column("role_requirement_id")]
    public Guid RoleRequirementId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("start_time")]
    public DateTimeOffset StartTime { get; set; }

    [Column("end_time")]
    public DateTimeOffset EndTime { get; set; }

    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
    public ICollection<QrCodeToken> QrCodeTokens { get; set; } = new List<QrCodeToken>();
}
