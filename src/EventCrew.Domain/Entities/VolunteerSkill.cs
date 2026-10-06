using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

/// <summary>
/// Join entity recording which skills a volunteer holds and their self-assessed proficiency.
/// Composite primary key: (VolunteerId, SkillId).
/// </summary>
[Table("volunteer_skills")]
public class VolunteerSkill
{
    [Column("volunteer_id")]
    public Guid VolunteerId { get; set; }

    [Column("skill_id")]
    public Guid SkillId { get; set; }

    /// <summary>
    /// Self-assessed proficiency level.
    /// Allowed values: Beginner | Intermediate | Advanced
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("proficiency_level")]
    public string ProficiencyLevel { get; set; } = "Intermediate";

    // Navigation
    public VolunteerProfile Volunteer { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}