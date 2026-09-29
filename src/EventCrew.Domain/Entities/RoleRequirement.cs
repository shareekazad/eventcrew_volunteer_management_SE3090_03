using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventCrew.Domain.Enums;

namespace EventCrew.Domain.Entities;

/// <summary>
/// A staffing requirement for an event (e.g., "Usher x 5").
/// </summary>
[Table("role_requirements")]
public class RoleRequirement
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("event_id")]
    public Guid EventId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("role_name")]
    public string RoleName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("required_headcount")]
    public int RequiredHeadcount { get; set; }

    [Column("min_experience_level")]
    [MaxLength(20)]
    public ExperienceLevel MinExperienceLevel { get; set; } = ExperienceLevel.Beginner;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    [ForeignKey(nameof(EventId))]
    public Event Event { get; set; } = null!;
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    public bool BelongsToEvent(Guid eventId) => EventId == eventId;
}