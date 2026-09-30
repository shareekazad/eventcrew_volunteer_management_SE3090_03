namespace EventCrew.Domain.Entities;

/// <summary>
/// Represents a skill that can be held by a volunteer.
/// Populated centrally (e.g., by admins) and referenced by volunteers via VolunteerSkill.
/// </summary>
public class Skill
{
    public Guid Id { get; set; }

    /// <summary>Human-readable skill name, e.g. "CPR Certified".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Grouping category, e.g. "Medical", "Technical", "Logistics".</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Optional free-text description of what the skill entails.</summary>
    public string? Description { get; set; }

    // Navigation
    public ICollection<VolunteerSkill> VolunteerSkills { get; set; } = new List<VolunteerSkill>();
}
