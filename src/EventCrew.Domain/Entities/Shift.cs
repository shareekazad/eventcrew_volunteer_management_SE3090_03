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

    [Column("description")]
    public string? Description { get; set; }

    [Column("start_time")]
    public DateTimeOffset StartTime { get; set; }

    [Column("end_time")]
    public DateTimeOffset EndTime { get; set; }

    [Column("capacity")]
    public int Capacity { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Scheduled";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(EventId))]
    public Event Event { get; set; } = null!;

    [ForeignKey(nameof(RoleRequirementId))]
    public RoleRequirement RoleRequirement { get; set; } = null!;

    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
    public ICollection<QrCodeToken> QrCodeTokens { get; set; } = new List<QrCodeToken>();
}
