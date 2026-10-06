using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

/// <summary>
/// Represents a skill that can be held by a volunteer.
/// Populated centrally (e.g., by admins) and referenced by volunteers via VolunteerSkill.
/// </summary>
[Table("skills")]
public class Skill
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-readable skill name, e.g. "CPR Certified".</summary>
    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Grouping category, e.g. "Medical", "Technical", "Logistics".</summary>
    [Required]
    [MaxLength(50)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Optional free-text description of what the skill entails.</summary>
    [Column("description")]
    public string? Description { get; set; }

    // Navigation
    public ICollection<VolunteerSkill> VolunteerSkills { get; set; } = new List<VolunteerSkill>();
}