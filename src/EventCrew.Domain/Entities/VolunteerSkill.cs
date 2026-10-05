using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

[Table("volunteer_skills")]
public class VolunteerSkill
{
    [Column("volunteer_id")]
    public Guid VolunteerId { get; set; }

    [Column("skill_id")]
    public Guid SkillId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("proficiency_level")]
    public string ProficiencyLevel { get; set; } = "Intermediate";
}
