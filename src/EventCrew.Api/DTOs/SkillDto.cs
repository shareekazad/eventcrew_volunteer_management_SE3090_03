namespace EventCrew.Api.DTOs;

/// <summary>
/// Lightweight skill representation returned inside a volunteer profile response.
/// </summary>
public class SkillDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    /// <summary>The volunteer's self-assessed proficiency for this skill.</summary>
    public string ProficiencyLevel { get; set; } = string.Empty;
}
