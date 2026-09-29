namespace EventCrew.Api.DTOs.Events;

/// <summary>
/// Response shape for a single role requirement nested inside an event.
/// </summary>
public class RoleRequirementDto
{
    public Guid Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RequiredHeadcount { get; set; }
    public string MinExperienceLevel { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}