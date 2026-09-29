namespace EventCrew.Domain.Entities;

/// <summary>
/// Join entity recording which skills a volunteer holds and their self-assessed proficiency.
/// Composite primary key: (VolunteerId, SkillId).
/// </summary>
public class VolunteerSkill
{
    public Guid VolunteerId { get; set; }
    public Guid SkillId { get; set; }

    /// <summary>
    /// Self-assessed proficiency level.
    /// Allowed values: Beginner | Intermediate | Advanced
    /// </summary>
    public string ProficiencyLevel { get; set; } = "Intermediate";

    // Navigation
    public VolunteerProfile Volunteer { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
