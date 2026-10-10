namespace EventCrew.Api.DTOs;

/// <summary>
/// Response shape for a skill in the catalog (not attached to a volunteer).
/// </summary>
public class SkillResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
}